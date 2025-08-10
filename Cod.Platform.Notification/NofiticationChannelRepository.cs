namespace Cod.Platform.Notification
{
    internal sealed class NofiticationChannelRepository(Lazy<IQueryableRepository<OpenID>> repository) : INofiticationChannelRepository
    {
        public IAsyncEnumerable<OpenID> GetChannelsAsync(Guid user, int kind, CancellationToken cancellationToken = default)
        {
            return repository.Value.QueryAsync(OpenID.BuildPartitionKey(user), OpenID.BuildRowKeyStart(kind), OpenID.BuildRowKeyEnd(kind), cancellationToken: cancellationToken);
        }
    }
}
