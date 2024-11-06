using Cod.Identity;

namespace Cod.Channel.Speech
{
    internal class SpeechRecognizeCommand(
        ISpeechRecognizer recognizer,
        IAuthenticator authenticator,
        ILoadingStateService loadingStateService)
        : ICommand<SpeechRecognizeCommandParameter, bool>
    {
        public async Task<bool> ExecuteAsync(SpeechRecognizeCommandParameter parameter, CancellationToken cancellationToken)
        {
            var permissions = await authenticator.GetResourcePermissionsAsync(cancellationToken);
            var resource = permissions.SingleOrDefault(p => p.Type == ResourceType.AzureSpeechService) ?? throw new ApplicationException(InternalError.Forbidden);

            var region = ParseAzureSpeechServiceRegion(resource.Resource);
            var token = await authenticator.RetrieveResourceTokenAsync(ResourceType.AzureSpeechService, resource.Resource, cancellationToken: cancellationToken);

            using (loadingStateService.SetBusy(BusyGroups.Speech))
            {
                return await recognizer.StartRecognitionAsync(
                    token ?? throw new ApplicationException(InternalError.AuthenticationRequired),
                    region, 
                    deviceID: parameter.InputSource, 
                    language: parameter.InputLanguage, 
                    cancellationToken: cancellationToken);
            }
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
