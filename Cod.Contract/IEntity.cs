using System;

namespace Cod
{
    public interface IEntity
    {
        string PartitionKey { get; }

        string RowKey { get; }

        string ETag { get; }

        DateTimeOffset Timestamp { get; }
    }
}
