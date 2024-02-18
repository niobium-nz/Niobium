using System;

namespace Cod.Model
{
    public class Business : IEntity
    {
        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public string ETag { get; set; }

        public DateTimeOffset? Timestamp { get; set; }

        public DateTimeOffset? Created { get; set; }

        public string Name { get; set; }

        public string Extra { get; set; }

        public string Brand { get; set; }

        public Guid Settler { get; set; }

        public static string BuildKey(Guid value)
        {
            return value.ToString("N").ToUpperInvariant();
        }

        public Guid GetParent()
        {
            return Guid.Parse(PartitionKey);
        }

        public void SetParent(Guid value)
        {
            PartitionKey = BuildKey(value);
        }

        public Guid GetID()
        {
            return Guid.Parse(RowKey);
        }

        public void SetID(Guid value)
        {
            RowKey = BuildKey(value);
        }
    }
}
