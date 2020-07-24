using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;

namespace Cod.Platform
{
    public class MemoryCachedRepository<T> : IRepository<T>
        where T : ITableEntity, IEntity, new()
    {
        private readonly Lazy<CloudTableRepository<T>> repository;
        private DateTimeOffset lastCached = DateTimeOffset.MinValue;

        public MemoryCachedRepository(Lazy<CloudTableRepository<T>> repository)
        {
            this.repository = repository;
        }

        protected List<T> Cache { get; private set; } = new List<T>();

        protected TimeSpan CacheRefreshInterval { get; set; } = TimeSpan.FromMinutes(10);

        public async Task<TableQueryResult<T>> GetAsync(int limit = -1)
        {
            await this.BuildCache();

            IList<T> result;
            if (limit > 0)
            {
                result = this.Cache.Take(limit).ToArray();
            }
            else
            {
                result = this.Cache.ToArray();
            }

            return new TableQueryResult<T>(result, null);
        }

        public async Task<TableQueryResult<T>> GetAsync(string partitionKey, int limit = -1)
        {
            await this.BuildCache();

            IList<T> result = this.Cache.Where(c => ((IEntity)c).PartitionKey == partitionKey).ToArray();
            if (limit > 0)
            {
                result = result.Take(limit).ToArray();
            }

            return new TableQueryResult<T>(result, null);
        }

        public async Task<T> GetAsync(string partitionKey, string rowKey)
        {
            await this.BuildCache();
            return this.Cache.SingleOrDefault(c => ((IEntity)c).PartitionKey == partitionKey && ((IEntity)c).RowKey == rowKey);
        }

        public async Task<IEnumerable<T>> CreateAsync(IEnumerable<T> entities, bool replaceIfExist)
            => await this.repository.Value.CreateAsync(entities, replaceIfExist);

        public async Task<IEnumerable<T>> CreateOrUpdateAsync(IEnumerable<T> entities)
            => await this.repository.Value.CreateOrUpdateAsync(entities);

        public async Task<IEnumerable<T>> DeleteAsync(IEnumerable<T> entities, bool successIfNotExist = false)
            => await this.repository.Value.DeleteAsync(entities, successIfNotExist);

        public async Task<IEnumerable<T>> UpdateAsync(IEnumerable<T> entities)
            => await this.repository.Value.UpdateAsync(entities);

        private async Task BuildCache()
        {
            if (DateTimeOffset.UtcNow - this.lastCached > this.CacheRefreshInterval)
            {
                var templates = await ((IRepository<T>)this.repository.Value).GetAsync();
                this.Cache.Clear();
                this.Cache.AddRange(templates);
            }
        }
    }
}
