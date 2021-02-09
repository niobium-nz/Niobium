using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        public Task<TDomain> GetAsync(TEntity entity) => this.GetAsync(entity, true);

        public Task<TDomain> GetAsync(string partitionKey, string rowKey)
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

        public async Task<IEnumerable<TDomain>> GetAsync(string partitionKey)
        {
            var stub = new List<TDomain>();

            var result = this.partitionCache.GetOrAdd(partitionKey, key => stub);

            if (stub.GetHashCode() == result.GetHashCode())
            {
                var entities = await this.repository.Value.GetAsync(partitionKey);

                for (var i = 0; i < entities.Count; i++)
                {
                    var d = await this.GetAsync(entities[i], false);
                    stub.Add(d);
                }
            }

            return result;
        }

        public async Task<IEnumerable<TDomain>> GetAsync()
        {
            var entities = await this.repository.Value.GetAsync();
            var result = new TDomain[entities.Count];

            for (var i = 0; i < entities.Count; i++)
            {
                result[i] = await this.GetAsync(entities[i]);
            }

            var partitions = result.GroupBy(d => d.PartitionKey);
            foreach (var partition in partitions)
            {
                this.partitionCache.AddOrUpdate(
                    partition.Key,
                    partition.ToList(),
                    (key, existing) =>
                    {
                        existing.Clear();
                        foreach (var item in partition)
                        {
                            existing.Add(item);
                        }
                        return existing;
                    });
            }

            return result;
        }

        public void Dispose()
        {
            if (!disposed)
            {
                this.instanceCache.Clear();
                this.partitionCache.Clear();
            }

            disposed = true;
        }

        protected Task<TDomain> GetAsync(TEntity entity, bool clearPartitionCache)
        {
            var domain = this.createDomain();
            domain.Initialize(entity);
            this.instanceCache.AddOrUpdate(entity.GetKey(), domain, (key, existing) => domain);

            if (clearPartitionCache)
            {
                this.partitionCache.TryRemove(entity.PartitionKey, out _);
            }

            return Task.FromResult(domain);
        }
    }
}
