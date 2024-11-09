using Cod.Identity;

namespace Cod.Channel.Speech
{
    public static class IAuthenticatorExtensions
    {
        public static async Task<(string, string)> GetSpeechSASAndRegionAsync(this IAuthenticator authenticator, CancellationToken cancellationToken = default)
        {
            var permissions = await authenticator.GetResourcePermissionsAsync(cancellationToken);
            var resource = permissions.SingleOrDefault(p => p.Type == ResourceType.AzureSpeechService) ?? throw new ApplicationException(InternalError.Forbidden);

            var region = ParseAzureSpeechServiceRegion(resource.Resource);
            var token = await authenticator.RetrieveResourceTokenAsync(ResourceType.AzureSpeechService, resource.Resource, cancellationToken: cancellationToken);
            return (token, region);
        }

        private static string ParseAzureSpeechServiceRegion(string azureSpeechServiceEndpoint)
        {
            if (string.IsNullOrWhiteSpace(azureSpeechServiceEndpoint))
            {
                throw new ArgumentNullException(nameof(azureSpeechServiceEndpoint));
            }

            var parts = azureSpeechServiceEndpoint.Split('.', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 0)
            {
                return parts[0];
            }
            else
            {
                throw new ArgumentException($"Invalid {nameof(azureSpeechServiceEndpoint)} found: {azureSpeechServiceEndpoint}");
            }
        }
    }
}
