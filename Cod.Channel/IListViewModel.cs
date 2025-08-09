namespace Cod.Channel
{
    public interface IListViewModel : IViewModel, IRefreshable, IEnumerable<IViewModel>
    {
        Task InitializeAsync(CancellationToken cancellationToken = default);
    }

    public interface IListViewModel<TViewModel, TDomain, TEntity> : IListViewModel
            where TViewModel : class, IViewModel<TDomain, TEntity>
            where TDomain : IDomain<TEntity>
    {
        IEnumerable<TViewModel> Children { get; }
    }
}
