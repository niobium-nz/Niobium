using Azure;
using Azure.Data.Tables;

namespace Cod.Platform.Database
{
    public class Cache : ITableEntity, IEntity
    {
        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public string ETag { get; set; }

        public DateTimeOffset? Timestamp { get; set; }

        public DateTimeOffset? Created { get; set; }

        public string Value { get; set; }

        public bool InMemory { get; set; }

        public DateTimeOffset Expiry { get; set; }

        ETag ITableEntity.ETag { get => new(ETag); set => ETag = value.ToString(); }
    }
}
