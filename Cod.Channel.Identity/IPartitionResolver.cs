using Cod.Identity;

namespace Cod.Channel.Identity
{
    public interface IPartitionResolver
    {
        Task<(bool Success, string? PartitionKey)> ResolvePartitionAsync(CancellationToken cancellationToken = default);
    }

    public class UserPartitionResolver : IPartitionResolver
    {
        private readonly Cod.Identity.IAuthenticator authenticator;

        public UserPartitionResolver(Cod.Identity.IAuthenticator authenticator)
        {
            this.authenticator = authenticator;
        }

        public async Task<(bool Success, string? PartitionKey)> ResolvePartitionAsync(CancellationToken cancellationToken = default)
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
