using System.Runtime.CompilerServices;

namespace Cod
{
    public class MemoryCachedRepository<T> : IRepository<T>
    {
        private static readonly TimeSpan CacheUpdateGap = TimeSpan.FromMinutes(5);
        private DateTimeOffset lastCacheUpdate = DateTimeOffset.MinValue;
        private readonly SemaphoreSlim cahceLock = new(1, 1);
        private readonly Dictionary<StorageKey, T> cache = new();
        private readonly IRepository<T> innerRepository;

        public MemoryCachedRepository(IRepository<T> innerRepository)
        {
            this.innerRepository = innerRepository;
        }

        public async Task<IEnumerable<T>> CreateAsync(IEnumerable<T> entities, bool replaceIfExist = false, DateTimeOffset? expiry = null, CancellationToken cancellationToken = default)
        {
            await cahceLock.WaitAsync(cancellationToken);
            try
            {
                var result = await innerRepository.CreateAsync(entities, replaceIfExist, expiry, cancellationToken);
                PurgeCache();
                return result;
            }
            finally
            {
                cahceLock.Release();
            }
        }

        public async Task DeleteAsync(IEnumerable<T> entities, bool preconditionCheck = true, bool successIfNotExist = true, CancellationToken cancellationToken = default)
        {
            await cahceLock.WaitAsync(cancellationToken);
            try
            {
                await innerRepository.DeleteAsync(entities, preconditionCheck, successIfNotExist, cancellationToken);
                PurgeCache();
            }
            finally
            {
                cahceLock.Release();
            }
        }

        public async Task<bool> ExistsAsync(string partitionKey, string rowKey, CancellationToken cancellationToken = default)
        {
            await cahceLock.WaitAsync(cancellationToken);
            try
            {
                await InitializeCacheAsync(cancellationToken);

                var key = new StorageKey { PartitionKey = partitionKey, RowKey = rowKey };
                return cache.ContainsKey(key);
            }
            finally
            {
                cahceLock.Release();
            }
        }

        public async Task<EntityBag<T>> GetAsync(int limit, string continuationToken = null, IList<string> fields = null, CancellationToken cancellationToken = default)
        {
            await cahceLock.WaitAsync(cancellationToken);
            try
            {
                await InitializeCacheAsync(cancellationToken);

                var result = cache.Values.Take(limit).ToList();
                return new EntityBag<T>(result, null);
            }
            finally
            {
                cahceLock.Release();
            }
        }

        public async Task<EntityBag<T>> GetAsync(string partitionKey, int limit, string continuationToken = null, IList<string> fields = null, CancellationToken cancellationToken = default)
        {
            await cahceLock.WaitAsync(cancellationToken);
            try
            {
                await InitializeCacheAsync(cancellationToken);

                var result = new List<T>();
                foreach (var key in cache.Keys)
                {
                    if (key.PartitionKey == partitionKey)
                    {
                        result.Add(cache[key]);
                    }
                }

                return new EntityBag<T>(result, null);
            }
            finally
            {
                cahceLock.Release();
            }
        }

        public async IAsyncEnumerable<T> GetAsync(IList<string> fields = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await cahceLock.WaitAsync(cancellationToken);
            try
            {
                await InitializeCacheAsync(cancellationToken);

                foreach (var item in cache.Values)
                {
                    yield return item;
                }
            }
            finally
            {
                cahceLock.Release();
            }
        }

        public async IAsyncEnumerable<T> GetAsync(string partitionKey, IList<string> fields = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await cahceLock.WaitAsync(cancellationToken);
            try
            {
                await InitializeCacheAsync(cancellationToken);

                var result = new List<T>();
                foreach (var key in cache.Keys)
                {
                    if (key.PartitionKey == partitionKey)
                    {
                        yield return cache[key];
                    }
                }
            }
            finally
            {
                cahceLock.Release();
            }
        }

        public async Task<T> RetrieveAsync(string partitionKey, string rowKey, IList<string> fields = null, CancellationToken cancellationToken = default)
        {
            await cahceLock.WaitAsync(cancellationToken);
            try
            {
                await InitializeCacheAsync(cancellationToken);

                var key = new StorageKey { PartitionKey = partitionKey, RowKey = rowKey };
                if (cache.TryGetValue(key, out var entity))
                {
                    return entity;
                }

                return default;
            }
            finally
            {
                cahceLock.Release();
            }
        }

        public async Task<IEnumerable<T>> UpdateAsync(IEnumerable<T> entities, bool preconditionCheck = true, bool mergeIfExists = false, CancellationToken cancellationToken = default)
        {
            await cahceLock.WaitAsync(cancellationToken);
            try
            {
                var result = await innerRepository.UpdateAsync(entities, preconditionCheck, mergeIfExists, cancellationToken);
                PurgeCache();
                return result;
            }
            finally
            {
                cahceLock.Release();
            }
        }

        protected async Task InitializeCacheAsync(CancellationToken cancellationToken = default)
        {
            if (cache.Count == 0 && DateTimeOffset.UtcNow - lastCacheUpdate > CacheUpdateGap)
            {
                var entities = innerRepository.GetAsync(cancellationToken: cancellationToken);
                await foreach (var item in entities)
                {
                    var pk = EntityMappingHelper.GetField<string>(item, EntityKeyKind.PartitionKey);
                    var rk = EntityMappingHelper.GetField<string>(item, EntityKeyKind.RowKey);
                    var key = new StorageKey { PartitionKey = pk, RowKey = rk };
                    cache.Add(key, item);
                }

                lastCacheUpdate = DateTimeOffset.UtcNow;
            }
        }

        protected virtual void PurgeCache() => cache.Clear();
    }
}
