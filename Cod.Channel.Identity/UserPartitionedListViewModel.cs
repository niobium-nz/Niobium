using Cod.Identity;

namespace Cod.Channel.Identity
{
    public class UserPartitionedListViewModel<TViewModel, TDomain, TEntity>(
        IAuthenticator authenticator,
        ILoadingStateService loadingStateService,
        ICommand<LoadCommandParameter, LoadCommandResult<TDomain>> loadCommand,
        ObjectFactory<TViewModel> viewModelFactory)
        : GenericListViewModel<TViewModel, TDomain, TEntity>(loadingStateService, loadCommand, viewModelFactory)
            where TViewModel : class, IViewModel<TDomain, TEntity>
            where TDomain : IDomain<TEntity>
    {
        protected string? Partition { get; private set; }

        protected override LoadCommandParameter LoadCommandParameter => new(Partition!);

        public override async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            Guid? user = await authenticator.GetUserIDAsync(cancellationToken);
            if (user == null || user == Guid.Empty)
            {
                return;
            }

            Partition = user.Value.ToKey();
            await RefreshAsync(cancellationToken);
            await base.InitializeAsync(cancellationToken);
        }
    }
}
