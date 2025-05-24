namespace Cod.Channel
{
    public abstract class GenericListViewModel<TViewModel, TDomain, TEntity>(
        ILoadingStateService loadingStateService,
        ICommand<LoadCommandParameter, LoadCommandResult<TDomain>> loadCommand,
        Func<TViewModel> createViewModel)
        : IRefreshable
            where TViewModel : class, IViewModel<TDomain, TEntity>
            where TDomain : IDomain<TEntity>
    {
        protected abstract LoadCommandParameter LoadCommandParameter { get; }
        protected IList<TViewModel> Children { get; set; } = [];
        public IEnumerable<TViewModel> List { get => Children; }

        public bool IsBusy => loadingStateService.IsBusy(typeof(TEntity).Name);

        public virtual async Task RefreshAsync(CancellationToken? cancellationToken = default)
        {
            cancellationToken ??= CancellationToken.None;

            var result = await loadCommand.ExecuteAsync(LoadCommandParameter, cancellationToken);

            Children = await Children.RefreshAsync(result.DomainsLoaded, createViewModel, default(TEntity), parent: this, cancellationToken: cancellationToken);
        }
    }
}
