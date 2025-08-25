using Azure.Messaging.ServiceBus;
using Microsoft.Net.Http.Headers;
using Niobium.Platform.Identity;
using System.Net.Http.Headers;
using System.Security.Claims;

namespace Niobium.Messaging.ServiceBus
{
    public static class PrincipalParserExtensions
    {
        public static async Task<ClaimsPrincipal?> ParseAsync(this PrincipalParser helper, ServiceBusReceivedMessage message, CancellationToken cancellationToken = default)
        {
            if (message.ApplicationProperties.TryGetValue(HeaderNames.Authorization, out object? authorization)
                && authorization != null && authorization is string auth
                && AuthenticationHeaderValue.TryParse(auth, out AuthenticationHeaderValue? authHeader))
            {
                if (authHeader.Scheme == AuthenticationScheme.BearerLoginScheme && !string.IsNullOrWhiteSpace(authHeader.Parameter))
                {
                    return await helper.ParseAsync(authHeader.Parameter, cancellationToken);
                }
            }

            return null;
        }
    }
}
