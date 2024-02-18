using System;

namespace Cod.Model
{
    public class Transaction : IEntity
    {
        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public DateTimeOffset? Timestamp { get; set; }

        public string ETag { get; set; }

        public double Delta { get; set; }

        public int Reason { get; set; }

        public string Remark { get; set; }

        public string Reference { get; set; }

        public string Corelation { get; set; }

        public string Account { get; set; }

        public int Status { get; set; }

        public int Provider { get; set; }

        public DateTimeOffset? Created { get; set; }

        public void SetOwner(string value)
        {
            PartitionKey = BuildPartitionKey(value);
        }

        public string GetOwner()
        {
            return PartitionKey;
        }

        public DateTimeOffset GetCreated()
        {
            return ParseRowKey(RowKey);
        }

        public static string BuildPartitionKey(string owner)
        {
            return owner.Trim();
        }

        public static string BuildRowKey(DateTimeOffset created)
        {
            return created.ToReverseUnixTimestamp();
        }

        public static string BuildRowKey(long created)
        {
            return DateTimeOffsetExtensions.FromReverseUnixTimeMilliseconds(created).ToReverseUnixTimestamp();
        }

        public static DateTimeOffset ParseRowKey(string input)
        {
            return DateTimeOffsetExtensions.FromReverseUnixTimeMilliseconds(long.Parse(input));
        }
    }
}