namespace Cod.Platform.Database
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
            IEnumerable<T> created = await tableRepository.CreateAsync(entities, replaceIfExist, expiry: expiry, cancellationToken: cancellationToken);
            foreach (T item in created)
            {
                IConvertible c = item.GetCache();
                if (c != null)
                {
                    DateTimeOffset itemExp = item.GetExpiry(DateTimeOffset.UtcNow);
                    DateTimeOffset cacheExp = expiry.HasValue && expiry.Value < itemExp ? expiry.Value : itemExp;
                    await cache.SetAsync(item.PartitionKey, item.RowKey, c.ToString(), memoryCached, expiry: cacheExp, cancellationToken: cancellationToken);
                }
            }
            return created;
        }

        public async Task<IEnumerable<T>> UpdateAsync(IEnumerable<T> entities, bool preconditionCheck = true, CancellationToken cancellationToken = default)
        {
            IEnumerable<T> updated = await tableRepository.UpdateAsync(entities, preconditionCheck: preconditionCheck, cancellationToken: cancellationToken);
            foreach (T item in updated)
            {
                IConvertible cv = item.GetCache();
                if (cv != null)
                {
                    DateTimeOffset itemExp = item.GetExpiry(DateTimeOffset.UtcNow);
                    await cache.SetAsync(item.PartitionKey, item.RowKey, cv.ToString(), memoryCached, itemExp, cancellationToken: cancellationToken);
                }
            }
            return updated;
        }

        public async Task<IEnumerable<T>> DeleteAsync(IEnumerable<T> entities, bool preconditionCheck = true, bool successIfNotExist = false, CancellationToken cancellationToken = default)
        {
            IEnumerable<T> deleted = await tableRepository.DeleteAsync(entities, preconditionCheck: preconditionCheck, successIfNotExist: successIfNotExist, cancellationToken: cancellationToken);
            foreach (T item in deleted)
            {
                await cache.DeleteAsync(item.PartitionKey, item.RowKey, cancellationToken);
            }
            return deleted;
        }

        public async Task<TableQueryResult<T>> GetAsync(int limit, string continuationToken = null, IList<string> fields = null, CancellationToken cancellationToken = default)
        {
            return await tableRepository.GetAsync(limit, continuationToken: continuationToken, fields: fields, cancellationToken: cancellationToken);
        }

        public async Task<TableQueryResult<T>> GetAsync(string partitionKey, int limit, string continuationToken = null, IList<string> fields = null, CancellationToken cancellationToken = default)
        {
            return await tableRepository.GetAsync(partitionKey, limit, continuationToken: continuationToken, fields: fields, cancellationToken: cancellationToken);
        }

        public IAsyncEnumerable<T> GetAsync(IList<string> fields = null, CancellationToken cancellationToken = default)
        {
            return tableRepository.GetAsync(fields: fields, cancellationToken: cancellationToken);
        }

        public IAsyncEnumerable<T> GetAsync(string partitionKey, IList<string> fields = null, CancellationToken cancellationToken = default)
        {
            return tableRepository.GetAsync(partitionKey, fields: fields, cancellationToken: cancellationToken);
        }

        public async Task<T> RetrieveAsync(string partitionKey, string rowKey, IList<string> fields = null, CancellationToken cancellationToken = default)
        {
            string c = await cache.GetAsync<string>(partitionKey, rowKey, cancellationToken: cancellationToken);
            if (c != null)
            {
                T result = new();
                result.Initialize(partitionKey, rowKey, c);
                return result;
            }
            T result2 = await tableRepository.RetrieveAsync(partitionKey, rowKey, fields: null, cancellationToken: cancellationToken);
            if (result2 != null)
            {
                IConvertible cv = result2.GetCache();
                if (cv != null)
                {
                    await cache.SetAsync(
                        result2.PartitionKey,
                        result2.RowKey,
                        cv.ToString(),
                        memoryCached,
                        result2.GetExpiry(DateTimeOffset.UtcNow),
                        cancellationToken: cancellationToken);
                }
            }
            return result2;
        }
    }
}
