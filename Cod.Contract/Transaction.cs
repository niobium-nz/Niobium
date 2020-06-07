using System;

namespace Cod
{
    public class Transaction : IEntity
    {
        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public string ETag { get; set; }

        public double Delta { get; set; }

        public int Reason { get; set; }

        public string Remark { get; set; }

        public string Reference { get; set; }

        public DateTimeOffset? Created { get; set; }

        public void SetOwner(string value) => this.PartitionKey = BuildPartitionKey(value);

        public string GetOwner() => this.PartitionKey;

        public static string BuildPartitionKey(string owner) => owner.Trim();

        public static string BuildRowKey(DateTimeOffset created) => created.ToReverseUnixTimestamp();

        public static string BuildRowKey(long created) => DateTimeOffsetExtensions.FromReverseUnixTimeMilliseconds(created).ToReverseUnixTimestamp();

        public static DateTimeOffset ParseRowKey(string input) => DateTimeOffsetExtensions.FromReverseUnixTimeMilliseconds(Int64.Parse(input));
    }
}