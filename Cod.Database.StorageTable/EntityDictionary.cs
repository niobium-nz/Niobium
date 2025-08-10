using Azure;
using Azure.Data.Tables;

namespace Cod.Database.StorageTable
{
    internal class EntityDictionary : Dictionary<string, object?>, ITableEntity
    {
        public string PartitionKey { get => Get<string>(nameof(PartitionKey))!; set => Set(nameof(PartitionKey), value); }

        public string RowKey { get => Get<string>(nameof(RowKey))!; set => Set(nameof(RowKey), value); }

        public DateTimeOffset? Timestamp { get => Get<DateTimeOffset?>(nameof(Timestamp)); set => Set(nameof(Timestamp), value); }

        public ETag ETag { get; set; }

        private T? Get<T>(string key)
        {
            return TryGetValue(key, out var value) ? value is T t ? t : default : default;
        }

        private void Set(string key, object? value)
        {
            if (value != null)
            {
                if (ContainsKey(key))
                {
                    this[key] = value;
                }
                else
                {
                    Add(key, value);
                }
            }
        }
    }
}
