using Cod.Platform.Integration.Azure;

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

        public async Task<IEnumerable<T>> CreateAsync(IEnumerable<T> entities, bool replaceIfExist, DateTimeOffset? expiry, CancellationToken cancellationToken = default)
        {
            var created = await this.tableRepository.CreateAsync(entities, replaceIfExist, expiry: expiry, cancellationToken: cancellationToken);
            foreach (var item in created)
            {
                var c = item.GetCache();
                if (c != null)
                {
                    var itemExp = item.GetExpiry(DateTimeOffset.UtcNow);
                    var cacheExp = expiry.HasValue && expiry.Value < itemExp ? expiry.Value : itemExp;
                    await this.cache.SetAsync(item.PartitionKey, item.RowKey, c.ToString(), this.memoryCached, expiry: cacheExp, cancellationToken: cancellationToken);
                }
            }
            return created;
        }

        public async Task<IEnumerable<T>> UpdateAsync(IEnumerable<T> entities, bool preconditionCheck = true, CancellationToken cancellationToken = default)
        {
            var updated = await this.tableRepository.UpdateAsync(entities, preconditionCheck: preconditionCheck, cancellationToken: cancellationToken);
            foreach (var item in updated)
            {
                var cv = item.GetCache();
                if (cv != null)
                {
                    var itemExp = item.GetExpiry(DateTimeOffset.UtcNow);
                    await this.cache.SetAsync(item.PartitionKey, item.RowKey, cv.ToString(), this.memoryCached, itemExp, cancellationToken: cancellationToken);
                }
            }
            return updated;
        }

        public async Task<IEnumerable<T>> DeleteAsync(IEnumerable<T> entities, bool preconditionCheck = true, bool successIfNotExist = false, CancellationToken cancellationToken = default)
        {
            var deleted = await this.tableRepository.DeleteAsync(entities, preconditionCheck: preconditionCheck, successIfNotExist: successIfNotExist, cancellationToken: cancellationToken);
            foreach (var item in deleted)
            {
                await this.cache.DeleteAsync(item.PartitionKey, item.RowKey, cancellationToken);
            }
            return deleted;
        }

        public async Task<TableQueryResult<T>> GetAsync(int limit, string continuationToken = null, IList<string> fields = null, CancellationToken cancellationToken = default)
            => await this.tableRepository.GetAsync(limit, continuationToken: continuationToken, fields: fields, cancellationToken: cancellationToken);

        public async Task<TableQueryResult<T>> GetAsync(string partitionKey, int limit, string continuationToken = null, IList<string> fields = null, CancellationToken cancellationToken = default)
            => await this.tableRepository.GetAsync(partitionKey, limit, continuationToken: continuationToken, fields: fields, cancellationToken: cancellationToken);

        public IAsyncEnumerable<T> GetAsync(IList<string> fields = null, CancellationToken cancellationToken = default)
            => this.tableRepository.GetAsync(fields: fields, cancellationToken: cancellationToken);

        public IAsyncEnumerable<T> GetAsync(string partitionKey, IList<string> fields = null, CancellationToken cancellationToken = default)
            => this.tableRepository.GetAsync(partitionKey, fields: fields, cancellationToken: cancellationToken);

        public async Task<T> RetrieveAsync(string partitionKey, string rowKey, IList<string> fields = null, CancellationToken cancellationToken = default)
        {
            var c = await this.cache.GetAsync<string>(partitionKey, rowKey, cancellationToken: cancellationToken);
            if (c != null)
            {
                var result = new T();
                result.Initialize(partitionKey, rowKey, c);
                return result;
            }
            var result2 = await this.tableRepository.RetrieveAsync(partitionKey, rowKey, fields: null, cancellationToken: cancellationToken);
            if (result2 != null)
            {
                var cv = result2.GetCache();
                if (cv != null)
                {
                    await this.cache.SetAsync(
                        result2.PartitionKey, 
                        result2.RowKey,
                        cv.ToString(),
                        this.memoryCached,
                        result2.GetExpiry(DateTimeOffset.UtcNow),
                        cancellationToken: cancellationToken);
                }
            }
            return result2;
        }
    }
}
