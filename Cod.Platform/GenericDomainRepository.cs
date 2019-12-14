using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cod.Contract;

namespace Cod.Platform
{
    public class GenericDomainRepository<TDomain, TEntity> : IDomainRepository<TDomain, TEntity>
        where TEntity : IEntity
        where TDomain : class, IPlatformDomain<TEntity>
    {
        private readonly Func<TDomain> createDomain;
        private readonly Lazy<IRepository<TEntity>> repository;

        public GenericDomainRepository(Func<TDomain> createDomain, Lazy<IRepository<TEntity>> repository)
        {
            this.createDomain = createDomain;
            this.repository = repository;
        }

        public Task<TDomain> CreateAsync(TEntity entity)
        {
            var domain = this.createDomain();
            domain.Initialize(entity);
            return Task.FromResult(domain);
        }

        public Task<TDomain> GetAsync(string partitionKey, string rowKey)
        {
            var domain = this.createDomain();
            domain.Initialize(partitionKey, rowKey);
            return Task.FromResult(domain);
        }

        public async Task<IEnumerable<TDomain>> GetAsync(string partitionKey)
        {
            var entities = await this.repository.Value.GetAsync(partitionKey);
            var result = new TDomain[entities.Count];

            for (var i = 0; i < entities.Count; i++)
            {
                result[i] = await this.CreateAsync(entities[i]);
            }

            return result;
        }

    }
}
