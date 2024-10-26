using Azure.Messaging.ServiceBus;
using Cod.Platform.Identity;
using Microsoft.Net.Http.Headers;
using System.Security.Claims;

namespace Cod.Messaging.ServiceBus
{
    public static class PrincipalParserExtensions
    {
        public static async Task<ClaimsPrincipal?> ParseAsync(this PrincipalParser helper, ServiceBusReceivedMessage message, CancellationToken cancellationToken = default)
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
                        return await helper.ParseAsync(token, cancellationToken);
                    }
                }
            }

            return null;
        }
    }
}
