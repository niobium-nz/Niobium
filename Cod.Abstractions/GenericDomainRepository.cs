using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Cod
{
    public class GenericDomainRepository<TDomain, TEntity>(Func<TDomain> createDomain, Lazy<IRepository<TEntity>> repository) : IDomainRepository<TDomain, TEntity>, IDisposable
        where TDomain : class, IDomain<TEntity>
    {
        private readonly List<TDomain> cachedDomains = [];
        private readonly List<string> cachedPartitions = [];
        private readonly ConcurrentDictionary<StorageKey, TDomain> instanceCache = new();
        private readonly ConcurrentDictionary<string, IList<TDomain>> partitionCache = new();
        private bool disposed;

        public IReadOnlyCollection<TDomain> CachedDomains => cachedDomains;

        public IReadOnlyCollection<string> CachedPartitions => cachedPartitions;

        public Task<TDomain> GetAsync(TEntity entity, CancellationToken? cancellationToken = default)
        {
            cancellationToken ??= CancellationToken.None;
            return GetAsync(entity, true, cancellationToken.Value);
        }

        public Task<TDomain> GetAsync(string partitionKey, string rowKey, bool forceLoad = false, CancellationToken? cancellationToken = default)
        {
            var key = new StorageKey { PartitionKey = partitionKey, RowKey = rowKey };

            if (forceLoad)
            {
                instanceCache.TryRemove(key, out _);
            }

            return Task.FromResult(instanceCache.GetOrAdd(key, k =>
                {
                    if (partitionCache.TryGetValue(k.PartitionKey, out var partition))
                    {
                        var c = partition.SingleOrDefault(d => d.RowKey == k.RowKey);
                        if (c != null)
                        {
                            return c;
                        }
                    }

                    TDomain domain = createDomain();
                    domain.Initialize(k.PartitionKey, k.RowKey);
                    return domain;
                }));
        }

        public async IAsyncEnumerable<TDomain> GetAsync(string partitionKey, bool forceLoad = false, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (forceLoad)
            {
                partitionCache.TryRemove(partitionKey, out _);
            }

            List<TDomain> stub = [];

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

                RebuildCache();
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
                if (domain.PartitionKey == null)
                {
                    throw new InvalidOperationException("Domain must have a valid PartitionKey.");
                }

                partitionCache.AddOrUpdate(
                    domain.PartitionKey,
                    [domain],
                    (key, existing) =>
                    {
                        existing.Add(domain);
                        return existing;
                    });
                yield return domain;
            }

            RebuildCache();
        }

        public Task<TDomain> BuildAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            TDomain domain = createDomain();
            domain.Initialize(entity);
            if (domain.PartitionKey == null)
            {
                throw new InvalidOperationException("Domain must have a valid PartitionKey.");
            }

            partitionCache.AddOrUpdate(
                domain.PartitionKey,
                [domain],
                (key, existing) =>
                {
                    existing.Add(domain);
                    return existing;
                });
            RebuildCache();

            return Task.FromResult(domain);
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
            ArgumentNullException.ThrowIfNull(entity);
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
                var changed = partitionCache.TryRemove(itemPK, out _);
                if (changed)
                {
                    RebuildCache();
                }
            }

            return Task.FromResult(domain);
        }

        protected void RebuildCache()
        {
            cachedPartitions.Clear();
            cachedDomains.Clear();

            var orderedPartitionKeys = partitionCache.Keys.OrderBy(k => k);
            foreach (var key in orderedPartitionKeys)
            {
                cachedPartitions.Add(key);
                cachedDomains.AddRange(partitionCache[key]);
            }
        }
    }
}
