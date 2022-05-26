namespace Cod.Platform
{
    public interface IDomainRepository<TDomain, TEntity>
        where TEntity : IEntity
        where TDomain : class, IDomain<TEntity>
    {
        Task<TDomain> GetAsync(string partitionKey, string rowKey);

        Task<TDomain> GetAsync(TEntity entity);

        Task<IEnumerable<TDomain>> GetAsync(string partitionKey);

        Task<IEnumerable<TDomain>> GetAsync();
    }
}
