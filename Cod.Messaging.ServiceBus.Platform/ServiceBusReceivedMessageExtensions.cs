using Azure.Messaging.ServiceBus;
using Cod.Platform.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using System.Security.Claims;

namespace Cod.Messaging.ServiceBus
{
    public static class ServiceBusReceivedMessageExtensions
    {
        public static async Task<ClaimsPrincipal?> TryParsePrincipalAsync(this ServiceBusReceivedMessage message, SecurityKey? key = null, CancellationToken cancellationToken = default)
        {
            if (message.ApplicationProperties.TryGetValue(HeaderNames.Authorization, out var authorization)
                && authorization != null && authorization is string auth)
            {
                var parts = auth.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    var scheme = parts[0];
                    if (scheme == AuthenticationScheme.BearerLoginScheme)
                    {
                        var token = auth[(scheme.Length + 1)..];
                        return await AccessTokenHelper.TryParsePrincipalAsync(token, key, cancellationToken: cancellationToken);
                    }
                }
            }

            return null;
        }
    }
}
