using Cod.Platform.Tenant;

namespace Cod.Platform.Notification
{
    internal class NofiticationChannelRepository : INofiticationChannelRepository
    {
        private readonly Lazy<IQueryableRepository<OpenID>> repository;

        public NofiticationChannelRepository(Lazy<IQueryableRepository<OpenID>> repository)
        {
            this.repository = repository;
        }

        public IAsyncEnumerable<OpenID> GetChannelsAsync(Guid user, int kind, CancellationToken cancellationToken = default)
        {
            return repository.Value.QueryAsync(OpenID.BuildPartitionKey(user), OpenID.BuildRowKeyStart(kind), OpenID.BuildRowKeyEnd(kind), cancellationToken: cancellationToken);
        }
    }
}
