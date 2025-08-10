namespace Cod
{
    public class StorageSignature
    {
        public required string Signature { get; set; }

        public required StorageControl Control { get; set; }

        public long Expiry { get; set; }

        public DateTimeOffset Expires => DateTimeOffset.FromUnixTimeSeconds(Expiry);
    }
}
