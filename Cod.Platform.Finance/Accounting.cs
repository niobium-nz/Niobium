namespace Cod.Platform.Finance
{
    public class Accounting
    {
        [EntityKey(EntityKeyKind.PartitionKey)]
        public string PartitionKey { get; set; }

        [EntityKey(EntityKeyKind.RowKey)]
        public string RowKey { get; set; }

        [EntityKey(EntityKeyKind.ETag)]
        public string ETag { get; set; }

        public double Credits { get; set; }

        public double Debits { get; set; }

        public double Balance { get; set; }

        public string GetPrincipal()
        {
            return PartitionKey;
        }

        public void SetPrincipal(string value)
        {
            PartitionKey = BuildPartitionKey(value);
        }

        public DateTimeOffset GetEnd()
        {
            return DateTimeOffsetExtensions.FromReverseUnixTimeMilliseconds(long.Parse(RowKey));
        }

        public void SetEnd(DateTimeOffset value)
        {
            RowKey = BuildRowKey(value);
        }

        public static string BuildPartitionKey(string principal)
        {
            return principal.Trim();
        }

        public static string BuildRowKey(DateTimeOffset end)
        {
            return end.ToReverseUnixTimestamp();
        }
    }
}