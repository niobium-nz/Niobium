namespace Niobium.Platform.Notification
{
    public interface INofiticationChannelRepository
    {
        IAsyncEnumerable<OpenID> GetChannelsAsync(Guid user, int kind, CancellationToken cancellationToken = default);
    }
}
