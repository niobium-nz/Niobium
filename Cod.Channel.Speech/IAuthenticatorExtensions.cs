using Cod.Identity;

namespace Cod.Channel.Speech
{
    public static class IAuthenticatorExtensions
    {
        public static async Task<(string, string)> GetSpeechSASAndRegionAsync(this IAuthenticator authenticator, CancellationToken cancellationToken = default)
        {
            IEnumerable<ResourcePermission> permissions = await authenticator.GetResourcePermissionsAsync(cancellationToken);
            ResourcePermission resource = permissions.SingleOrDefault(p => p.Type == ResourceType.AzureSpeechService) ?? throw new ApplicationException(InternalError.Forbidden);

            string region = ParseAzureSpeechServiceRegion(resource.Resource);
            string token = await authenticator.RetrieveResourceTokenAsync(ResourceType.AzureSpeechService, resource.Resource, cancellationToken: cancellationToken);
            return (token, region);
        }

        private static string ParseAzureSpeechServiceRegion(string azureSpeechServiceEndpoint)
        {
            if (string.IsNullOrWhiteSpace(azureSpeechServiceEndpoint))
            {
                throw new ArgumentNullException(nameof(azureSpeechServiceEndpoint));
            }

            string[] parts = azureSpeechServiceEndpoint.Split('.', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 0
                ? parts[0]
                : throw new ArgumentException($"Invalid {nameof(azureSpeechServiceEndpoint)} found: {azureSpeechServiceEndpoint}");
        }
    }
}
