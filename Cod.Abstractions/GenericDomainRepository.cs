using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Cod
{
    public class GenericDomainRepository<TDomain, TEntity> : IDomainRepository<TDomain, TEntity>, IDisposable
        where TDomain : class, IDomain<TEntity>
    {
        private readonly ConcurrentDictionary<StorageKey, TDomain> instanceCache;
        private readonly ConcurrentDictionary<string, IList<TDomain>> partitionCache;
        private readonly Func<TDomain> createDomain;
        private readonly Lazy<IRepository<TEntity>> repository;
        private bool disposed;

        public GenericDomainRepository(Func<TDomain> createDomain, Lazy<IRepository<TEntity>> repository)
        {
            instanceCache = new ConcurrentDictionary<StorageKey, TDomain>();
            partitionCache = new ConcurrentDictionary<string, IList<TDomain>>();
            this.createDomain = createDomain;
            this.repository = repository;
        }

        public Task<TDomain> GetAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            return GetAsync(entity, true, cancellationToken);
        }

        public Task<TDomain> GetAsync(string partitionKey, string rowKey, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(instanceCache.GetOrAdd(
                new StorageKey { PartitionKey = partitionKey, RowKey = rowKey },
                key =>
                {
                    if (partitionCache.TryGetValue(key.PartitionKey, out IList<TDomain> partition))
                    {
                        TDomain c = partition.SingleOrDefault(d => d.RowKey == key.RowKey);
                        if (c != null)
                        {
                            return c;
                        }
                    }

                    TDomain domain = createDomain();
                    domain.Initialize(key.PartitionKey, key.RowKey);
                    return domain;
                }));
        }

        public async IAsyncEnumerable<TDomain> GetAsync(string partitionKey, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            List<TDomain> stub = new();

            IList<TDomain> result = partitionCache.GetOrAdd(partitionKey, key => stub);

            if (stub.GetHashCode() == result.GetHashCode())
            {
                IAsyncEnumerable<TEntity> entities = repository.Value.GetAsync(partitionKey, cancellationToken: cancellationToken);
                await foreach (TEntity entity in entities)
                {
                    TDomain d = await GetAsync(entity, false, cancellationToken);
                    stub.Add(d);
                    yield return d;
                }
            }
            else
            {
                foreach (TDomain item in result)
                {
                    yield return item;
                }
            }
        }

        public async IAsyncEnumerable<TDomain> GetAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            IAsyncEnumerable<TEntity> entities = repository.Value.GetAsync(cancellationToken: cancellationToken);
            await foreach (TEntity entity in entities)
            {
                TDomain domain = await GetAsync(entity, cancellationToken);
                partitionCache.AddOrUpdate(
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
            if (!disposed)
            {
                instanceCache.Clear();
                partitionCache.Clear();
            }

            disposed = true;
            GC.SuppressFinalize(this);
        }

        protected Task<TDomain> GetAsync(TEntity entity, bool clearPartitionCache, CancellationToken cancellationToken)
        {
            TDomain domain = createDomain();
            domain.Initialize(entity);

            // REMARK (5he11) 可能存在一种情况是接口使用者仅仅为了拿到一个空白的Domain Object所以仅仅传入一个无用的new Entity()，此时这个entity的GetKey返回null
            string itemPK = EntityMappingHelper.GetField<string>(entity, EntityKeyKind.PartitionKey);
            string itemRK = EntityMappingHelper.GetField<string>(entity, EntityKeyKind.RowKey);
            if (itemPK != null && itemRK != null)
            {
                instanceCache.AddOrUpdate(new StorageKey { PartitionKey = itemPK, RowKey = itemRK }, domain, (key, existing) => domain);
            }

            if (clearPartitionCache && itemPK != null)
            {
                partitionCache.TryRemove(itemPK, out _);
            }

            return Task.FromResult(domain);
        }
    }
}
