namespace Cod
{
    public interface IDomainRepository<TDomain, TEntity>
        where TDomain : class, IDomain<TEntity>
    {
        IReadOnlyCollection<string> CachedPartitions { get; }

        IReadOnlyCollection<TDomain> CachedDomains { get; }

        Task<TDomain> GetAsync(string partitionKey, string rowKey, bool forceLoad = false, CancellationToken? cancellationToken = default);

        Task<TDomain> GetAsync(TEntity entity, CancellationToken? cancellationToken = default);

        IAsyncEnumerable<TDomain> GetAsync(string partitionKey, bool forceLoad = false, CancellationToken cancellationToken = default);

        IAsyncEnumerable<TDomain> GetAsync(CancellationToken cancellationToken = default);
    }
}
