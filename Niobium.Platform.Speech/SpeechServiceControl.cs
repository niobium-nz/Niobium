using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace Niobium.Platform.Speech
{
    internal sealed class SpeechServiceControl(IOptions<SpeechServiceOptions> options) : IResourceControl
    {
        public bool Grantable(ResourceType type, string resource)
        {
            return type == ResourceType.AzureSpeechService && resource == options.Value.FullyQualifiedDomainName;
        }

        public Task<StorageControl?> GrantAsync(ClaimsPrincipal principal, ResourceType type, string resource, string? partition, string? row, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<StorageControl?>(new StorageControl(default, resource));
        }
    }
}
