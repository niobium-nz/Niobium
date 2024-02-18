using System;

namespace Cod
{
    public class StorageSignature
    {
        public string Signature { get; set; }

        public StorageControl Control { get; set; }

        public long Expiry { get; set; }

        public DateTimeOffset Expires => DateTimeOffset.FromUnixTimeSeconds(Expiry);
    }
}
