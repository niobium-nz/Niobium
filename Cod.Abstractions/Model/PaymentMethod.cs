using System;

namespace Cod.Model
{
    public class PaymentMethod : IEntity
    {
        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public string ETag { get; set; }

        public DateTimeOffset? Timestamp { get; set; }

        public DateTimeOffset? Created { get; set; }

        public string Identity { get; set; }

        public DateTimeOffset Expires { get; set; }

        public int Channel { get; set; }

        public int Kind { get; set; }

        public int Status { get; set; }

        public bool Primary { get; set; }

        public string Extra { get; set; }

        public static string BuildPartitionKey(Guid user)
            => user.ToString("N").ToUpperInvariant();

        public static string BuildRowKey(string id) => id.Trim();

        public Guid GetUser() => Guid.Parse(this.PartitionKey);

        public string GetID() => this.RowKey;
    }
}
