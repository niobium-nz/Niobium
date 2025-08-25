using Microsoft.Extensions.Options;

namespace Niobium.Platform.Speech
{
    internal sealed class SpeechServiceSignatureIssuer(
        IOptions<SpeechServiceOptions> options,
        HttpClient httpClient)
        : ISignatureIssuer
    {
        private static readonly TimeSpan speechServiceSASValidity = TimeSpan.FromMinutes(10);

        public bool CanIssue(ResourceType type, StorageControl control)
        {
            return type == ResourceType.AzureSpeechService && control.Resource == options.Value.FullyQualifiedDomainName;
        }

        public async Task<(string, DateTimeOffset)> IssueAsync(ResourceType type, StorageControl control, DateTimeOffset expires, CancellationToken cancellationToken = default)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            HttpResponseMessage response = await httpClient.PostAsync("/sts/v1.0/issueToken", null, cancellationToken);
            string sas = await response.Content.ReadAsStringAsync(cancellationToken);
            DateTimeOffset exp = now.Add(speechServiceSASValidity);
            return (sas, exp);
        }
    }
}
