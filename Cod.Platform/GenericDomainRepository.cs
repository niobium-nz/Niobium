using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Cod.Platform
{
    public class GenericDomainRepository<TDomain, TEntity> : IDomainRepository<TDomain, TEntity>, IDisposable
        where TEntity : IEntity
        where TDomain : class, IPlatformDomain<TEntity>
    {
        private readonly ConcurrentDictionary<StorageKey, TDomain> instanceCache;
        private readonly ConcurrentDictionary<string, IList<TDomain>> partitionCache;
        private readonly Func<TDomain> createDomain;
        private readonly Lazy<IRepository<TEntity>> repository;
        private bool disposed;

        public GenericDomainRepository(Func<TDomain> createDomain, Lazy<IRepository<TEntity>> repository)
        {
            this.instanceCache = new ConcurrentDictionary<StorageKey, TDomain>();
            this.partitionCache = new ConcurrentDictionary<string, IList<TDomain>>();
            this.createDomain = createDomain;
            this.repository = repository;
        }

        public Task<TDomain> GetAsync(TEntity entity, CancellationToken cancellationToken = default) => this.GetAsync(entity, true, cancellationToken);

        public Task<TDomain> GetAsync(string partitionKey, string rowKey, CancellationToken cancellationToken = default)
            => Task.FromResult(this.instanceCache.GetOrAdd(
                new StorageKey { PartitionKey = partitionKey, RowKey = rowKey },
                key =>
                {
                    if (this.partitionCache.TryGetValue(key.PartitionKey, out var partition))
                    {
                        var c = partition.SingleOrDefault(d => d.RowKey == key.RowKey);
                        if (c != null)
                        {
                            return c;
                        }
                    }

                    var domain = this.createDomain();
                    domain.Initialize(key.PartitionKey, key.RowKey);
                    return domain;
                }));

        public async IAsyncEnumerable<TDomain> GetAsync(string partitionKey, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var stub = new List<TDomain>();

            var result = this.partitionCache.GetOrAdd(partitionKey, key => stub);

            if (stub.GetHashCode() == result.GetHashCode())
            {
                var entities = this.repository.Value.GetAsync(partitionKey, cancellationToken: cancellationToken);
                await foreach (var entity in entities)
                {
                    var d = await this.GetAsync(entity, false, cancellationToken);
                    stub.Add(d);
                    yield return d;
                }
            }
            else
            {
                foreach (var item in result)
                {
                    yield return item;
                }
            }
        }

        public async IAsyncEnumerable<TDomain> GetAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var entities = this.repository.Value.GetAsync(cancellationToken: cancellationToken);
            await foreach (var entity in entities)
            {
                var domain = await this.GetAsync(entity, cancellationToken);
                this.partitionCache.AddOrUpdate(
                    domain.PartitionKey,
                    new List<TDomain> { domain },
                    (key, existing) =>
                    {
                        existing.Add(domain);
                        return existing;
                    });
                yield return domain;
            }
        }

        public void Dispose()
        {
            if (!this.disposed)
            {
                this.instanceCache.Clear();
                this.partitionCache.Clear();
            }

            this.disposed = true;
            GC.SuppressFinalize(this);
        }

        protected Task<TDomain> GetAsync(TEntity entity, bool clearPartitionCache, CancellationToken cancellationToken)
        {
            var domain = this.createDomain();
            domain.Initialize(entity);

            // REMARK (5he11) 可能存在一种情况是接口使用者仅仅为了拿到一个空白的Domain Object所以仅仅传入一个无用的new Entity()，此时这个entity的GetKey返回null
            if (entity.PartitionKey != null && entity.RowKey != null)
            {
                this.instanceCache.AddOrUpdate(entity.GetKey(), domain, (key, existing) => domain);
            }

            if (clearPartitionCache && entity.PartitionKey != null)
            {
                this.partitionCache.TryRemove(entity.PartitionKey, out _);
            }

            return Task.FromResult(domain);
        }
    }
}
