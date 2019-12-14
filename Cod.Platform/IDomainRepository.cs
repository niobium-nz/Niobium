using System.Collections.Generic;
using System.Threading.Tasks;
using Cod.Contract;

namespace Cod.Platform
{
    public interface IDomainRepository<TDomain, TEntity>
        where TEntity : IEntity
        where TDomain : class, IDomain<TEntity>
    {
        Task<TDomain> GetAsync(string partitionKey, string rowKey);

        Task<IEnumerable<TDomain>> GetAsync(string partitionKey);

        Task<TDomain> CreateAsync(TEntity entity);
    }
}
