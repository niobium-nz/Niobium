using System;

namespace Cod.Model
{
    public class Interest : IEntity
    {
        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public string ETag { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public DateTimeOffset? Created { get; set; }

        public int Kind { get; set; }

        public bool Percentage { get; set; }

        public int Value { get; set; }

        public int Agreement { get; set; }

        public int Condition { get; set; }

        public string Target { get; set; }

        public static string BuildPartitionKey(Guid ownerBusiness) => ownerBusiness.ToKey();

        public static string BuildRowKey() => DateTimeOffset.UtcNow.ToReverseUnixTimestamp();
    }
}
