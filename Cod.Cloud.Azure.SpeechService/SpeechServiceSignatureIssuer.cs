using Cod.Platform;

namespace Cod.Cloud.Azure.SpeechService
{
    internal class SpeechServiceSignatureIssuer(SpeechServiceOptions options, HttpClient httpClient) : ISignatureIssuer
    {
        private static readonly TimeSpan speechServiceSASValidity = TimeSpan.FromMinutes(10);

        public bool CanIssue(ResourceType type, StorageControl control)
            => type == ResourceType.AzureSpeechService && control.Resource == options.ServiceEndpoint;

        public async Task<(string, DateTimeOffset)> IssueAsync(ResourceType type, StorageControl control, DateTimeOffset expires, CancellationToken cancellationToken = default)
        {
            var now = DateTimeOffset.UtcNow;
            var response = await httpClient.PostAsync("/sts/v1.0/issueToken", null, cancellationToken);
            var sas = await response.Content.ReadAsStringAsync(cancellationToken);
            var exp = now.Add(speechServiceSASValidity);
            return (sas, exp);
        }
    }
}
