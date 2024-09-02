namespace Cod.Platform.Tenant
{
    public class Business : ITrackable
    {
        [EntityKey(EntityKeyKind.PartitionKey)]
        public string PartitionKey { get; set; }

        [EntityKey(EntityKeyKind.RowKey)]
        public string RowKey { get; set; }

        [EntityKey(EntityKeyKind.Timestamp)]
        public DateTimeOffset? Timestamp { get; set; }

        [EntityKey(EntityKeyKind.ETag)]
        public string ETag { get; set; }

        public DateTimeOffset? Created { get; set; }

        public string Name { get; set; }

        public string Extra { get; set; }

        public string Brand { get; set; }

        public Guid Settler { get; set; }

        public static string BuildKey(Guid value)
        {
            return value.ToString("N").ToUpperInvariant();
        }

        public Guid GetParent()
        {
            return Guid.Parse(PartitionKey);
        }

        public void SetParent(Guid value)
        {
            PartitionKey = BuildKey(value);
        }

        public Guid GetID()
        {
            return Guid.Parse(RowKey);
        }

        public void SetID(Guid value)
        {
            RowKey = BuildKey(value);
        }
    }
}
