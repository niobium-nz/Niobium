namespace Cod.Platform
{
    public interface IDomainRepository<TDomain, TEntity>
        where TEntity : IEntity
        where TDomain : class, IDomain<TEntity>
    {
        Task<TDomain> GetAsync(string partitionKey, string rowKey, CancellationToken cancellationToken = default);

        Task<TDomain> GetAsync(TEntity entity, CancellationToken cancellationToken = default);

        IAsyncEnumerable<TDomain> GetAsync(string partitionKey, CancellationToken cancellationToken = default);

        IAsyncEnumerable<TDomain> GetAsync(CancellationToken cancellationToken = default);
    }
}
