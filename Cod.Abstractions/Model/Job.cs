using System;

namespace Cod.Model
{
    public class Job : IEntity
    {
        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public string ETag { get; set; }

        public DateTimeOffset? Timestamp { get; set; }

        public DateTimeOffset? Created { get; set; }

        public DateTimeOffset Expiry { get; set; }

        public string Reference { get; set; }
    }
}
