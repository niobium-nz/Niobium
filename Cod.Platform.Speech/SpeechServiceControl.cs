using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace Cod.Platform.Speech
{
    internal class SpeechServiceControl(IOptions<SpeechServiceOptions> options) : IResourceControl
    {
        public bool Grantable(ResourceType type, string resource)
            => type == ResourceType.AzureSpeechService && resource == options.Value.ServiceEndpoint;

        public Task<StorageControl> GrantAsync(ClaimsPrincipal principal, ResourceType type, string resource, string partition, string row, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new StorageControl(default, resource));
        }
    }
}
