namespace Niobium.Channel
{
    public interface IViewModel : IRefreshable
    {
        IRefreshable? Parent { get; }

        bool IsInitialized { get; }

        bool IsBusy { get; }

        EventHandler? RefreshRequested { get; set; }
    }

    public interface IViewModel<TDomain, TEntity> : IViewModel
        where TDomain : IDomain<TEntity>
    {
        string? PartitionKey { get; }

        string? RowKey { get; }

        Task<string?> GetHashAsync(CancellationToken cancellationToken = default);

        Task<IViewModel<TDomain, TEntity>> InitializeAsync(TDomain domain, IRefreshable? parent = null, bool force = false, CancellationToken cancellationToken = default);
    }
}
