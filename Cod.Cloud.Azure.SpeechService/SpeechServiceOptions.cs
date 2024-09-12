namespace Cod.Cloud.Azure.SpeechService
{
    public class SpeechServiceOptions()
    {
        public required string ServiceRegion { get; set; }

        public required string ServiceEndpoint { get; set; }

        public required string AccessKey { get; set; }

        public bool Validate()
            => !string.IsNullOrWhiteSpace(ServiceEndpoint)
                && !string.IsNullOrWhiteSpace(AccessKey)
                && !string.IsNullOrWhiteSpace(ServiceRegion);
    }
}
