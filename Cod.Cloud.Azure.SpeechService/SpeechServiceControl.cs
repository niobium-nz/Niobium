using Cod.Platform;
using System.Security.Claims;

namespace Cod.Cloud.Azure.SpeechService
{
    internal class SpeechServiceControl(SpeechServiceOptions options) : IResourceControl
    {
        public bool Grantable(ResourceType type, string resource)
            => type == ResourceType.AzureSpeechService && resource == options.ServiceEndpoint;

        public Task<StorageControl> GrantAsync(ClaimsPrincipal principal, ResourceType type, string resource, string partition, string row, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new StorageControl(default, resource));
        }
    }
}
