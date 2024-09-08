namespace Cod.Platform.Identity
{
    public class Profile : ITrackable
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

        public static string BuildPartitionKey(Guid tenant, Guid user)
        {
            return $"{tenant.ToKey()}|{user.ToKey()}";
        }

        public static bool TryParse(string partitionKey, out Guid tenant, out Guid user)
        {
            tenant = default;
            user = default;
            if (partitionKey is null)
            {
                throw new ArgumentNullException(nameof(partitionKey));
            }

            string[] splited = partitionKey.Split('|');
            return splited.Length == 2 && Guid.TryParse(splited[0], out tenant) && Guid.TryParse(splited[1], out user);
        }

        public Guid GetTenant()
        {
            return TryParse(PartitionKey, out Guid business, out Guid _) ? business : throw new NotSupportedException();
        }

        public Guid GetUser()
        {
            return TryParse(PartitionKey, out Guid _, out Guid user) ? user : throw new NotSupportedException();
        }
    }
}
