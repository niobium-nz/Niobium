using Azure;
using Azure.Data.Tables;

namespace Cod.Storage.Table
{
    public class EntityDictionary : Dictionary<string, object>, ITableEntity
    {
        public string PartitionKey { get => Get<string>(nameof(PartitionKey)); set => Set(nameof(PartitionKey), value); }

        public string RowKey { get => Get<string>(nameof(RowKey)); set => Set(nameof(RowKey), value); }

        public DateTimeOffset? Timestamp { get => Get<DateTimeOffset?>(nameof(Timestamp)); set => Set(nameof(Timestamp), value); }

        public ETag ETag { get => new(Get<string>(nameof(ETag))); set => Set(nameof(ETag), value.ToString()); }

        private T Get<T>(string key)
        {
            return TryGetValue(key, out object value) ? (T)value : default;
        }

        private void Set(string key, object value)
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
