using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cod.Platform
{
    public class CachedRepository<T> : IRepository<T> where T : ICachableEntity, new()
    {
        private readonly IRepository<T> tableRepository;
        private readonly ICacheStore cache;
        private readonly bool memoryCached;

        public CachedRepository(IRepository<T> tableRepository, ICacheStore cache, bool memoryCached)
        {
            this.tableRepository = tableRepository;
            this.cache = cache;
            this.memoryCached = memoryCached;
        }

        public async Task<IEnumerable<T>> CreateAsync(IEnumerable<T> entities, bool replaceIfExist)
        {
            var created = await this.tableRepository.CreateAsync(entities, replaceIfExist);
            foreach (var item in created)
            {
                var c = item.GetCache();
                if (c != null)
                {
                    await this.cache.SetAsync(item.PartitionKey, item.RowKey, c.ToString(), this.memoryCached);
                }
            }
            return created;
        }

        public async Task<IEnumerable<T>> DeleteAsync(IEnumerable<T> entities)
        {
            var deleted = await this.tableRepository.DeleteAsync(entities);
            foreach (var item in deleted)
            {
                await this.cache.DeleteAsync(item.PartitionKey, item.RowKey);
            }
            return deleted;
        }

        public async Task<TableQueryResult<T>> GetAsync(int limit)
            => await this.tableRepository.GetAsync(limit);

        public async Task<TableQueryResult<T>> GetAsync(string partitionKey, int limit)
            => await this.tableRepository.GetAsync(partitionKey, limit);

        public async Task<T> GetAsync(string partitionKey, string rowKey)
        {
            var c = await this.cache.GetAsync<string>(partitionKey, rowKey);
            if (c != null)
            {
                var result = new T();
                result.Initialize(partitionKey, rowKey, c);
                return result;
            }
            var result2 = await this.tableRepository.GetAsync(partitionKey, rowKey);
            if (result2 != null)
            {
                var cv = result2.GetCache();
                if (cv != null)
                {
                    await this.cache.SetAsync(result2.PartitionKey, result2.RowKey, cv.ToString(),
                        this.memoryCached, result2.GetExpiry(DateTimeOffset.UtcNow));
                }
            }
            return result2;
        }

        public async Task<IEnumerable<T>> UpdateAsync(IEnumerable<T> entities)
        {
            var updated = await this.tableRepository.UpdateAsync(entities);
            foreach (var item in updated)
            {
                var cv = item.GetCache();
                if (cv != null)
                {
                    await this.cache.SetAsync(item.PartitionKey, item.RowKey, cv.ToString(), this.memoryCached);
                }
            }
            return updated;
        }
    }
}
