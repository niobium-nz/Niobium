using System;
using Microsoft.Azure.Cosmos.Table;

namespace Cod.Platform.Model
{
    public class QueueMessage : TableEntity, ICloneable
    {
        public object Body { get; set; }

        public TimeSpan? Delay { get; set; }

        public object Clone() => new QueueMessage
        {
            Body = this.Body,
            Delay = this.Delay,
            PartitionKey = this.PartitionKey,
            RowKey = this.RowKey,
        };
    }
}
