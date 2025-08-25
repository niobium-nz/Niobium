using Microsoft.Extensions.Options;
using Niobium.Identity;

namespace Niobium.Platform.Speech
{
    internal sealed class RoleBasedTranscribeEntitlementDescriptor(string roleToGrant, IOptions<SpeechServiceOptions> options) : IEntitlementDescriptor
    {
        public bool IsHighOverhead => false;

        public bool CanDescribe(Guid tenant, Guid user, string role)
        {
            return roleToGrant == role;
        }

        public Task<IEnumerable<EntitlementDescription>> DescribeAsync(Guid tenant, Guid user, string role)
        {
            return Task.FromResult<IEnumerable<EntitlementDescription>>(
            [
                new()
                {
                    Permission = "TRANSCRIBE",
                    Resource = options.Value.FullyQualifiedDomainName,
                    Type = ResourceType.AzureSpeechService,
                }
            ]);
        }
    }
}
