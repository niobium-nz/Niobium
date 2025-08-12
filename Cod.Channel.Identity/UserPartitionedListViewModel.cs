namespace Cod.Channel.Identity
{
    public class UserPartitionedListViewModel<TViewModel, TDomain, TEntity>(
        IPartitionResolver partitionResolver,
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
            (bool success, Partition) = await partitionResolver.ResolvePartitionAsync(cancellationToken);
            if (!success)
            {
                return;
            }

            await RefreshAsync(cancellationToken);
            await base.InitializeAsync(cancellationToken);
        }
    }
}
