namespace Cod.Finance
{
    public class PaymentMethod : ITrackable
    {
        [EntityKey(EntityKeyKind.PartitionKey)]
        public required string PartitionKey { get; set; }

        [EntityKey(EntityKeyKind.RowKey)]
        public required string RowKey { get; set; }

        [EntityKey(EntityKeyKind.Timestamp)]
        public DateTimeOffset? Timestamp { get; set; }

        [EntityKey(EntityKeyKind.ETag)]
        public string? ETag { get; set; }

        public DateTimeOffset? Created { get; set; }

        public required string Identity { get; set; }

        public DateTimeOffset Expires { get; set; }

        public int Channel { get; set; }

        public int Kind { get; set; }

        public int Status { get; set; }

        public bool Primary { get; set; }

        public string? Extra { get; set; }

        public static string BuildPartitionKey(string user)
        {
            return user.Trim();
        }

        public static string BuildRowKey(string id)
        {
            return id.Trim();
        }

        public Guid GetUser()
        {
            return Guid.Parse(PartitionKey);
        }

        public string GetID()
        {
            return RowKey;
        }
    }
}
