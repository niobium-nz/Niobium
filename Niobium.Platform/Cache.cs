namespace Niobium.Platform
{
    public class Cache
    {
        [EntityKey(EntityKeyKind.PartitionKey)]
        public required string PartitionKey { get; set; }

        [EntityKey(EntityKeyKind.RowKey)]
        public required string RowKey { get; set; }

        [EntityKey(EntityKeyKind.ETag)]
        public string? ETag { get; set; }

        public required string Value { get; set; }

        public bool InMemory { get; set; }

        public DateTimeOffset Expiry { get; set; }
    }
}
