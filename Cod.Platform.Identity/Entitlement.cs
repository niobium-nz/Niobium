namespace Cod.Platform.Identity
{
    public class Entitlement : ITrackable
    {
        [EntityKey(EntityKeyKind.PartitionKey)]
        public string PartitionKey { get; set; }

        [EntityKey(EntityKeyKind.RowKey)]
        public string RowKey { get; set; }

        [EntityKey(EntityKeyKind.Timestamp)]
        public DateTimeOffset? Timestamp { get; set; }

        [EntityKey(EntityKeyKind.ETag)]
        public string ETag { get; set; }

        public string Value { get; set; }

        public DateTimeOffset? Created { get; set; }

        public static string BuildPartitionKey(Guid user)
        {
            return user.ToKey();
        }

        public static string BuildRowKey(string entitlement)
        {
            return $"{Entitlements.CategoryNamingPrefix}{entitlement.Trim().ToUpperInvariant()}";
        }

        public static string BuildRowKey(string entitlement, ushort offset)
        {
            return offset <= 0
                ? throw new NotSupportedException()
                : $"{Entitlements.CategoryNamingPrefix}{entitlement.Trim().ToUpperInvariant()}-{offset}";
        }

        public Guid GetUser()
        {
            return Guid.Parse(PartitionKey);
        }

        public string GetKey()
        {
            return RowKey.Trim();
        }
    }
}
