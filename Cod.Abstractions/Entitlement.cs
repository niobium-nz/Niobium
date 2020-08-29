using System;

namespace Cod
{
    public class Entitlement : IEntity
    {
        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public string ETag { get; set; }

        public string Value { get; set; }

        public DateTimeOffset? Created { get; set; }

        public static string BuildPartitionKey(Guid user) => user.ToKey();

        public static string BuildRowKey(string entitlement) => $"{Entitlements.CategoryNamingPrefix}{entitlement.Trim().ToUpperInvariant()}";

        public static string BuildRowKey(string entitlement, ushort offset)
        {
            if (offset <= 0)
            {
                throw new NotSupportedException();
            }
            return $"{Entitlements.CategoryNamingPrefix}{entitlement.Trim().ToUpperInvariant()}-{offset}";
        }

        public Guid GetUser() => Guid.Parse(this.PartitionKey);

        public string GetKey() => this.RowKey.Trim();
    }
}
