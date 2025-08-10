using Cod.Identity;

namespace Cod.Channel.Identity
{
    public class UserPartitionedListViewModel<TViewModel, TDomain, TEntity>(
        IAuthenticator authenticator,
        ILoadingStateService loadingStateService,
        ICommand<LoadCommandParameter, LoadCommandResult<TDomain>> loadCommand,
        Func<TViewModel> createViewModel)
        : GenericListViewModel<TViewModel, TDomain, TEntity>(loadingStateService, loadCommand, createViewModel)
            where TViewModel : class, IViewModel<TDomain, TEntity>
            where TDomain : IDomain<TEntity>
    {
        protected string? Partition { get; private set; }

        protected override LoadCommandParameter LoadCommandParameter => new(Partition!);

        public override async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            (bool success, Partition) = await GetPartitionAsync(cancellationToken);
            if (!success)
            {
                return;
            }

            await RefreshAsync(cancellationToken);
            await base.InitializeAsync(cancellationToken);
        }

        protected virtual async Task<(bool, string?)> GetPartitionAsync(CancellationToken cancellationToken)
        {
            Guid? user = await authenticator.GetUserIDAsync(cancellationToken);
            return user switch
            {
                null => (false, null),
                var id when id == Guid.Empty => (false, null),
                var id => (true, id.Value.ToKey())
            };
        }
    }
}
