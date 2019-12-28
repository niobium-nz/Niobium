using System;

namespace Cod
{
    public class Accounting : IEntity
    {
        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public string ETag { get; set; }

        public double Credits { get; set; }

        public double Debits { get; set; }

        public double Balance { get; set; }

        public string GetPrincipal() => this.PartitionKey;

        public void SetPrincipal(string value) => this.PartitionKey = BuildPartitionKey(value);

        public DateTimeOffset GetEnd() => DateTimeOffsetExtensions.FromReverseUnixTimeMilliseconds(Int64.Parse(this.RowKey));

        public void SetEnd(DateTimeOffset value) => this.RowKey = BuildRowKey(value);

        public static string BuildPartitionKey(string principal) => principal.Trim();

        public static string BuildRowKey(DateTimeOffset end) => end.ToReverseUnixTimestamp();
    }
}