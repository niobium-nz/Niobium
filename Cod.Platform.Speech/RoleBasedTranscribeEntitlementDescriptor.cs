using Cod.Identity;
using Microsoft.Extensions.Options;

namespace Cod.Platform.Speech
{
    internal class RoleBasedTranscribeEntitlementDescriptor(string roleToGrant, IOptions<SpeechServiceOptions> options) : IEntitlementDescriptor
    {
        public bool IsHighOverhead => false;

        public bool CanDescribe(Guid tenant, Guid user, string role) => roleToGrant == role;

        public Task<IEnumerable<EntitlementDescription>> DescribeAsync(Guid tenant, Guid user, string role)
        {
            return Task.FromResult<IEnumerable<EntitlementDescription>>(
            [
                new()
                {
                    Permission = "*:Transcribe",
                    Resource = options.Value.ServiceEndpoint,
                    Type = ResourceType.AzureSpeechService,
                }
            ]);
        }
    }
}
