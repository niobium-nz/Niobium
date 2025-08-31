using Azure.Messaging.ServiceBus;
using Niobium.Platform.Identity;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;

namespace Niobium.Platform.ServiceBus
{
    public static class ServiceBusReceivedMessageExtensions
    {
        private const string JsonMediaType = "application/json";

        public static bool TryParse<T>(this ServiceBusReceivedMessage message, [NotNullWhen(true)] out T? request, [NotNullWhen(true)] out string? rawbody)
        {
            request = default;
            rawbody = default;

            if (message.ContentType != JsonMediaType)
            {
                return false;
            }

            rawbody = message.Body.ToString();
            if (string.IsNullOrWhiteSpace(rawbody))
            {
                return false;
            }

            request = JsonMarshaller.Unmarshall<T>(rawbody);
            return request != null;
        }

        public static async Task<Guid> TryGetUserIDAsync(this ServiceBusReceivedMessage message, PrincipalParser principalParser, CancellationToken cancellationToken = default)
        {
            string? claim = await message.TryGetClaimAsync<string>(principalParser, ClaimTypes.NameIdentifier, cancellationToken);
            return claim == null || !Guid.TryParse(claim, out Guid userID) ? throw new ApplicationException(Niobium.InternalError.Forbidden) : userID;
        }

        public static async Task<T?> TryGetClaimAsync<T>(this ServiceBusReceivedMessage message, PrincipalParser principalParser, string key, CancellationToken cancellationToken = default)
        {
            IEnumerable<T> claims = await message.TryGetClaimsAsync<T>(principalParser, key, cancellationToken);
            return claims.SingleOrDefault();
        }

        public static async Task<IEnumerable<T>> TryGetClaimsAsync<T>(this ServiceBusReceivedMessage message, PrincipalParser principalParser, string key, CancellationToken cancellationToken = default)
        {
            ClaimsPrincipal principal = await principalParser.ParseAsync(message, cancellationToken: cancellationToken)
                ?? throw new ApplicationException(Niobium.InternalError.AuthenticationRequired);

            IEnumerable<Claim> claims = principal.Claims.Where(c => c.Type == key);
            return !claims.Any() ? [] : claims.Select(c => TypeConverter.Convert<T>(c.Value));
        }
    }
}
