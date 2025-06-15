namespace Cod.Channel
{
    public interface IViewModel<TDomain, TEntity>
        where TDomain : IDomain<TEntity>
    {
        string PartitionKey { get; }

        string RowKey { get; }

        Task<string> GetHashAsync(CancellationToken cancellationToken = default);

        IRefreshable Parent { get; }

        Task<IViewModel<TDomain, TEntity>> InitializeAsync(TDomain domain, IRefreshable parent = null, bool force = false, CancellationToken cancellationToken = default);
    }
}
