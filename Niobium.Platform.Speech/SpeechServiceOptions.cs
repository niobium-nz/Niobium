namespace Niobium.Platform.Speech
{
    public class SpeechServiceOptions
    {
        public required string FullyQualifiedDomainName { get; set; }

        public required string AccessKey { get; set; }

        public string ServiceRegion => FullyQualifiedDomainName.Split('.')[0];

        public bool Validate()
        {
            return !string.IsNullOrWhiteSpace(FullyQualifiedDomainName) && !string.IsNullOrWhiteSpace(AccessKey);
        }
    }
}
