using System;

namespace Cod
{
    public class Business : IEntity
    {
        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public string ETag { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public DateTimeOffset? Created { get; set; }

        public string Name { get; set; }

        public string Extra { get; set; }

        public string Brand { get; set; }

        public Guid Settler { get; set; }

        public static string BuildKey(Guid value) => value.ToString("N").ToUpperInvariant();

        public Guid GetParent() => Guid.Parse(this.PartitionKey);

        public void SetParent(Guid value) => this.PartitionKey = BuildKey(value);

        public Guid GetID() => Guid.Parse(this.RowKey);

        public void SetID(Guid value) => this.RowKey = BuildKey(value);
    }
}
