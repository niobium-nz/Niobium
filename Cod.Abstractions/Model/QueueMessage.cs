using System;

namespace Cod.Model
{
    public class QueueMessage : IEntity, ICloneable
    {
        public object Body { get; set; }

        public TimeSpan? Delay { get; set; }

        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public string ETag { get; set; }

        public DateTimeOffset? Created { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public object Clone() => new QueueMessage
        {
            Body = this.Body,
            Delay = this.Delay,
            PartitionKey = this.PartitionKey,
            RowKey = this.RowKey,
            ETag = this.ETag,
            Timestamp = this.Timestamp,
        };
    }
}
