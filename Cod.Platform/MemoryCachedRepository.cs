using Cod.Platform.Integration.Azure;
using System.Runtime.CompilerServices;

namespace Cod.Platform
{
    public class MemoryCachedRepository<T> : IRepository<T>, IDisposable
        where T : IEntity
    {
        private readonly IRepository<T> repository;
        private readonly SemaphoreSlim locker = new(1, 1);
        private bool disposed;
        private DateTimeOffset lastCached = DateTimeOffset.MinValue;

        public MemoryCachedRepository(IRepository<T> repository) => this.repository = repository;

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
            if (fields != null)
            {
                throw new NotSupportedException($"{typeof(MemoryCachedRepository<T>).Name} does not support query with explicit fields.");
            }

            return this.BuildCache(cancellationToken);
        }

        public async IAsyncEnumerable<T> GetAsync(string partitionKey, IList<string> fields = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (fields != null)
            {
                throw new NotSupportedException($"{typeof(MemoryCachedRepository<T>).Name} does not support query with explicit fields.");
            }

            var result = this.BuildCache(cancellationToken);
            await foreach (var item in result)
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

            var result = this.BuildCache(cancellationToken);
            await foreach (var item in result)
            {
                if (item.PartitionKey == partitionKey && item.RowKey == rowKey)
                {
                    return item;
                }
            }

            return default;
        }

        public async Task<IEnumerable<T>> CreateAsync(IEnumerable<T> entities, bool replaceIfExist, DateTimeOffset? expiry, CancellationToken cancellationToken = default)
            => await this.repository.CreateAsync(entities, replaceIfExist, expiry: expiry, cancellationToken: cancellationToken);

        public async Task<IEnumerable<T>> DeleteAsync(IEnumerable<T> entities, bool preconditionCheck = true, bool successIfNotExist = false, CancellationToken cancellationToken = default)
            => await this.repository.DeleteAsync(entities, preconditionCheck: preconditionCheck, successIfNotExist: successIfNotExist, cancellationToken: cancellationToken);

        public async Task<IEnumerable<T>> UpdateAsync(IEnumerable<T> entities, bool preconditionCheck = true, CancellationToken cancellationToken = default)
            => await this.repository.UpdateAsync(entities, preconditionCheck: preconditionCheck, cancellationToken: cancellationToken);

        private async IAsyncEnumerable<T> BuildCache([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await this.locker.WaitAsync(cancellationToken);
            try
            {
                if (DateTimeOffset.UtcNow - this.lastCached > this.CacheRefreshInterval)
                {
                    this.Cache.Clear();
                    this.lastCached = DateTimeOffset.UtcNow;

                    var templates = this.repository.GetAsync(cancellationToken: cancellationToken);
                    await foreach (var template in templates)
                    {
                        this.Cache.Add(template);
                        yield return template;
                    }
                }
                else
                {
                    foreach (var template in this.Cache)
                    {
                        yield return template;
                    }
                }
            }
            finally
            {
                _ = this.locker.Release();
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!this.disposed)
                {
                    this.locker.Dispose();
                }
            }

            this.disposed = true;
        }
    }
}
