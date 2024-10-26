using System.Reflection;

namespace Cod.Database.StorageTable
{
    internal static class DBEntityHelper
    {
        private static readonly Dictionary<string, string> AzureTableEntityMapping = new()
        {
            { "odata.etag", EntityKeyKind.ETag.ToString() },
        };

        public static EntityDictionary ToTableEntity(object source)
        {
            EntityDictionary dic = new();
            Type type = source.GetType();
            IReadOnlyDictionary<string, PropertyInfo> m = EntityMappingHelper.GetMapping(type);
            foreach (string key in m.Keys)
            {
                if (key == EntityKeyKind.Timestamp.ToString())
                {
                    continue;
                }

                object value = m[key].GetValue(source);
                if (key == EntityKeyKind.PartitionKey.ToString() || key == EntityKeyKind.RowKey.ToString())
                {
                    value = value.ToString();
                }

                if (key == EntityKeyKind.ETag.ToString())
                {
                    if (value != null)
                    {
                        dic.ETag = new((string)value);
                    }
                }
                else
                {
                    dic.Add(key, value);
                }
            }

            return dic;
        }

        public static T FromTableEntity<T>(this IDictionary<string, object> source) where T : class, new()
        {
            return source.ToObject<T>(AzureTableEntityMapping);
        }

        private static T ToObject<T>(this IDictionary<string, object> source, Dictionary<string, string> specialMapping)
            where T : class, new()
        {
            T obj = new();
            Type type = obj.GetType();
            var mapping = EntityMappingHelper.GetMapping(type);

            foreach (KeyValuePair<string, object> item in source)
            {
                string keyName = specialMapping.TryGetValue(item.Key, out string mappedKey) ? mappedKey : item.Key;
                if (mapping.TryGetValue(keyName, out PropertyInfo value))
                {
                    object itemValue = item.Value;
                    if (keyName == EntityKeyKind.Timestamp.ToString() && itemValue is long epoch)
                    {
                        itemValue = epoch < 9999999999 ? DateTimeOffset.FromUnixTimeSeconds(epoch) : DateTimeOffset.FromUnixTimeMilliseconds(epoch);
                    }

                    if ((keyName == EntityKeyKind.PartitionKey.ToString() || keyName == EntityKeyKind.RowKey.ToString())
                        && value.PropertyType != typeof(string))
                    {
                        itemValue = TypeConverter.Convert((string)itemValue, value.PropertyType);
                    }

                    value.SetValue(obj, itemValue);
                }
            }

            return obj;
        }
    }
}
