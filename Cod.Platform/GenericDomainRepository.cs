using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cod.Contract;

namespace Cod.Platform
{
    public class GenericDomainRepository<TDomain, TEntity> : IDomainRepository<TDomain, TEntity>
        where TEntity : IEntity
        where TDomain : class, IDomain<TEntity>
    {
        private readonly IRepository<TEntity> repository;
        private readonly Func<TDomain> createDomain;

        public GenericDomainRepository(IRepository<TEntity> repository, Func<TDomain> createDomain)
        {
            this.repository = repository;
            this.createDomain = createDomain;
        }

        public Task<TDomain> CreateAsync(TEntity entity)
        {
            var domain = this.createDomain();
            domain.Initialize(entity);
            return Task.FromResult(domain);
        }

        public async Task<TDomain> GetAsync(string partitionKey, string rowKey)
        {
            var entity = await this.repository.GetAsync(partitionKey, rowKey);
            if (entity == null)
            {
                return default;
            }

            return await this.CreateAsync(entity);
        }

        public async Task<IEnumerable<TDomain>> GetAsync(string partitionKey)
        {
            var entities = await this.repository.GetAsync(partitionKey);
            var result = new TDomain[entities.Count];

            for (var i = 0; i < entities.Count; i++)
            {
                result[i] = await this.CreateAsync(entities[i]);
            }

            return result;
        }

    }
}
