namespace Cod.Platform
{
    public class MemoryCachedRepository<T> : IRepository<T>, IDisposable
        where T : IEntity
    {
        private static readonly T[] EmptyCache = Array.Empty<T>();
        private readonly IRepository<T> repository;
        private readonly SemaphoreSlim locker = new(1, 1);
        private bool disposed;
        private DateTimeOffset lastCached = DateTimeOffset.MinValue;

        public MemoryCachedRepository(IRepository<T> repository) => this.repository = repository;

        protected List<T> Cache { get; private set; } = new List<T>();

        protected TimeSpan CacheRefreshInterval { get; set; } = TimeSpan.FromMinutes(10);

        public async Task<TableQueryResult<T>> GetAsync(int limit = -1)
        {
            await this.BuildCache();

            IList<T> result;
            var cacheCopy = this.Cache.Count == 0 ? EmptyCache : this.Cache.ToArray();
            result = limit > 0 && cacheCopy.Length >= limit ? cacheCopy.Take(limit).ToArray() : (IList<T>)cacheCopy;

            return new TableQueryResult<T>(result, null);
        }

        public async Task<TableQueryResult<T>> GetAsync(string partitionKey, int limit = -1)
        {
            await this.BuildCache();

            var cacheCopy = this.Cache.Count == 0 ? EmptyCache : this.Cache.ToArray();
            IList<T> result = cacheCopy.Where(c => c.PartitionKey == partitionKey).ToArray();
            if (limit > 0 && result.Count >= limit)
            {
                result = result.Take(limit).ToArray();
            }

            return new TableQueryResult<T>(result, null);
        }

        public async Task<T> GetAsync(string partitionKey, string rowKey)
        {
            await this.BuildCache();
            var cacheCopy = this.Cache.Count == 0 ? EmptyCache : this.Cache.ToArray();
            return cacheCopy.SingleOrDefault(c => c.PartitionKey == partitionKey && c.RowKey == rowKey);
        }

        public async Task<IEnumerable<T>> CreateAsync(IEnumerable<T> entities, bool replaceIfExist)
            => await this.repository.CreateAsync(entities, replaceIfExist);

        public async Task<IEnumerable<T>> CreateOrUpdateAsync(IEnumerable<T> entities)
            => await this.repository.CreateOrUpdateAsync(entities);

        public async Task<IEnumerable<T>> DeleteAsync(IEnumerable<T> entities, bool successIfNotExist = false)
            => await this.repository.DeleteAsync(entities, successIfNotExist);

        public async Task<IEnumerable<T>> UpdateAsync(IEnumerable<T> entities)
            => await this.repository.UpdateAsync(entities);

        private async Task BuildCache()
        {
            await this.locker.WaitAsync();
            try
            {
                if (DateTimeOffset.UtcNow - this.lastCached > this.CacheRefreshInterval)
                {
                    var templates = await this.repository.GetAsync();
                    this.Cache.Clear();
                    this.Cache.AddRange(templates);
                    this.lastCached = DateTimeOffset.UtcNow;
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
