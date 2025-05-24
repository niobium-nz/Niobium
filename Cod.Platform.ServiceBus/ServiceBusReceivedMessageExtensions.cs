using Azure.Messaging.ServiceBus;
using Cod.Platform.Identity;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using System.Text.Json;

namespace Cod.Messaging.ServiceBus
{
    public static class ServiceBusReceivedMessageExtensions
    {
        private const string JsonMediaType = "application/json";
        private static readonly JsonSerializerOptions SerializationOptions = new(JsonSerializerDefaults.Web);

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

            request = Deserialize<T>(rawbody);
            if (request == null)
            {
                return false;
            }

            return true;
        }

        public static async Task<Guid> TryGetUserIDAsync(this ServiceBusReceivedMessage message, PrincipalParser principalParser, CancellationToken cancellationToken = default)
        {
            var claim = await message.TryGetClaimAsync<string>(principalParser, ClaimTypes.NameIdentifier, cancellationToken);
            if (claim == null || !Guid.TryParse(claim, out var userID))
            {
                throw new ApplicationException(InternalError.Forbidden);
            }

            return userID;
        }

        public static async Task<T?> TryGetClaimAsync<T>(this ServiceBusReceivedMessage message, PrincipalParser principalParser, string key, CancellationToken cancellationToken = default)
        {
            var claims = await message.TryGetClaimsAsync<T>(principalParser, key, cancellationToken);
            return claims.SingleOrDefault();
        }

        public static async Task<IEnumerable<T>> TryGetClaimsAsync<T>(this ServiceBusReceivedMessage message, PrincipalParser principalParser, string key, CancellationToken cancellationToken = default)
        {
            var principal = await principalParser.ParseAsync(message, cancellationToken: cancellationToken)
                ?? throw new ApplicationException(InternalError.AuthenticationRequired);

            var claims = principal.Claims.Where(c => c.Type == key);
            if (!claims.Any())
            {
                return [];
            }

            return claims.Select(c => TypeConverter.Convert<T>(c.Value));
        }

        private static T Deserialize<T>(string json) => System.Text.Json.JsonSerializer.Deserialize<T>(json, SerializationOptions)!;
    }
}
