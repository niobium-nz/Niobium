using System.Runtime.CompilerServices;

namespace Cod.Platform.Database
{
    public class MemoryCachedRepository<T> : IRepository<T>, IDisposable
        where T : IEntity
    {
        private readonly IRepository<T> repository;
        private readonly SemaphoreSlim locker = new(1, 1);
        private bool disposed;
        private DateTimeOffset lastCached = DateTimeOffset.MinValue;

        public MemoryCachedRepository(IRepository<T> repository)
        {
            this.repository = repository;
        }

        protected List<T> Cache { get; private set; } = new List<T>();

        protected TimeSpan CacheRefreshInterval { get; set; } = TimeSpan.FromMinutes(10);

        public Task<TableQueryResult<T>> GetAsync(int limit, string continuationToken = null, IList<string> fields = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException($"Does not support paged query over memory-cached repository.");
        }

        public Task<TableQueryResult<T>> GetAsync(string partitionKey, int limit, string continuationToken = null, IList<string> fields = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException($"Does not support paged query over memory-cached repository.");
        }

        public IAsyncEnumerable<T> GetAsync(IList<string> fields = null, CancellationToken cancellationToken = default)
        {
            return fields != null
                ? throw new NotSupportedException($"{typeof(MemoryCachedRepository<T>).Name} does not support query with explicit fields.")
                : BuildCache(cancellationToken);
        }

        public async IAsyncEnumerable<T> GetAsync(string partitionKey, IList<string> fields = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (fields != null)
            {
                throw new NotSupportedException($"{typeof(MemoryCachedRepository<T>).Name} does not support query with explicit fields.");
            }

            IAsyncEnumerable<T> result = BuildCache(cancellationToken);
            await foreach (T item in result)
            {
                if (item.PartitionKey == partitionKey)
                {
                    yield return item;
                }
            }
        }

        public async Task<T> RetrieveAsync(string partitionKey, string rowKey, IList<string> fields = null, CancellationToken cancellationToken = default)
        {
            if (fields != null)
            {
                throw new NotSupportedException($"{typeof(MemoryCachedRepository<T>).Name} does not support query with explicit fields.");
            }

            IAsyncEnumerable<T> result = BuildCache(cancellationToken);
            await foreach (T item in result)
            {
                if (item.PartitionKey == partitionKey && item.RowKey == rowKey)
                {
                    return item;
                }
            }

            return default;
        }

        public async Task<IEnumerable<T>> CreateAsync(IEnumerable<T> entities, bool replaceIfExist, DateTimeOffset? expiry, CancellationToken cancellationToken = default)
        {
            return await repository.CreateAsync(entities, replaceIfExist, expiry: expiry, cancellationToken: cancellationToken);
        }

        public async Task<IEnumerable<T>> DeleteAsync(IEnumerable<T> entities, bool preconditionCheck = true, bool successIfNotExist = false, CancellationToken cancellationToken = default)
        {
            return await repository.DeleteAsync(entities, preconditionCheck: preconditionCheck, successIfNotExist: successIfNotExist, cancellationToken: cancellationToken);
        }

        public async Task<IEnumerable<T>> UpdateAsync(IEnumerable<T> entities, bool preconditionCheck = true, CancellationToken cancellationToken = default)
        {
            return await repository.UpdateAsync(entities, preconditionCheck: preconditionCheck, cancellationToken: cancellationToken);
        }

        private async IAsyncEnumerable<T> BuildCache([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await locker.WaitAsync(cancellationToken);
            try
            {
                if (DateTimeOffset.UtcNow - lastCached > CacheRefreshInterval)
                {
                    Cache.Clear();
                    lastCached = DateTimeOffset.UtcNow;

                    IAsyncEnumerable<T> templates = repository.GetAsync(cancellationToken: cancellationToken);
                    await foreach (T template in templates)
                    {
                        Cache.Add(template);
                        yield return template;
                    }
                }
                else
                {
                    foreach (T template in Cache)
                    {
                        yield return template;
                    }
                }
            }
            finally
            {
                _ = locker.Release();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!disposed)
                {
                    locker.Dispose();
                }
            }

            disposed = true;
        }
    }
}
