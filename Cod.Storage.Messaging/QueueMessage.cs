namespace Cod.Storage.Messaging
{
    public class QueueMessage : ICloneable
    {
        public object Body { get; set; }

        public TimeSpan? Delay { get; set; }

        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public string ETag { get; set; }

        public bool RetryDisabled { get; set; }

        public DateTimeOffset? Timestamp { get; set; }

        public object Clone()
        {
            return new QueueMessage
            {
                Body = Body,
                Delay = Delay,
                PartitionKey = PartitionKey,
                RowKey = RowKey,
                ETag = ETag,
                Timestamp = Timestamp,
            };
        }
    }
}
