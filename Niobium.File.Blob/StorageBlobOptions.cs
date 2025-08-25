namespace Niobium.File.Blob
{
    public class StorageBlobOptions
    {
        public string? FullyQualifiedDomainName { get; set; }

        public string? Key { get; set; }

        public int MaxRetries { get; set; } = 5;

        public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(5);

        public TimeSpan? ConnectionIdleTimeout { get; set; }

        public bool EnableInteractiveIdentity { get; set; }
    }
}
