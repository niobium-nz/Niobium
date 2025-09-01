using System.Collections;

namespace Niobium.Channel
{
    public abstract class GenericListViewModel<TViewModel, TDomain, TEntity>(
        ILoadingStateService loadingStateService,
        ICommand<LoadCommandParameter, LoadCommandResult<TDomain>> loadCommand,
        ObjectFactory<TViewModel> viewModelFactory)
        : IListViewModel<TViewModel, TDomain, TEntity>
            where TViewModel : class, IViewModel<TDomain, TEntity>
            where TDomain : IDomain<TEntity>
    {
        protected abstract LoadCommandParameter LoadCommandParameter { get; }

        protected IList<TViewModel> ViewModels { get; set; } = [];

        public IEnumerable<TViewModel> Children => ViewModels;

        public bool IsBusy => loadingStateService.IsBusy(typeof(TEntity).Name);

        public virtual IRefreshable? Parent => null;

        public bool IsInitialized { get; private set; }

        public EventHandler? RefreshRequested { get; set; }

        public string? ErrorMessage { get; protected set; }

        public virtual Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            IsInitialized = true;
            return Task.CompletedTask;
        }

        public virtual async Task RefreshAsync(CancellationToken cancellationToken = default)
        {
            LoadCommandResult<TDomain> result = await loadCommand.ExecuteAsync(LoadCommandParameter, cancellationToken);
            ViewModels = await ViewModels.RefreshAsync(result.DomainsLoaded, viewModelFactory, default(TEntity), parent: this, cancellationToken: cancellationToken);
        }

        public IEnumerator<IViewModel> GetEnumerator()
        {
            return Children.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
