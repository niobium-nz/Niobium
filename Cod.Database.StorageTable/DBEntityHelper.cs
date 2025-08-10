using System.Reflection;

namespace Cod.Database.StorageTable
{
    internal static class DBEntityHelper
    {
        private static readonly Dictionary<string, string> AzureTableEntityMapping = new()
        {
            { Constants.AzureTableETagKey, EntityKeyKind.ETag.ToString() },
        };

        public static EntityDictionary ToTableEntity(object source)
        {
            EntityDictionary dic = [];

            if (source is IEnumerable<KeyValuePair<string, object>> kvs)
            {
                foreach (KeyValuePair<string, object> kv in kvs)
                {
                    if (kv.Key == EntityKeyKind.ETag.ToString())
                    {
                        if (kv.Value != null)
                        {
                            dic.ETag = new((string)kv.Value);
                        }
                    }
                    else
                    {
                        dic.Add(kv.Key, kv.Value);
                    }
                }

                return dic;
            }

            Type type = source.GetType();
            IReadOnlyDictionary<string, PropertyInfo> m = EntityMappingHelper.GetMapping(type);
            foreach (string key in m.Keys)
            {
                if (key == EntityKeyKind.Timestamp.ToString())
                {
                    continue;
                }

                object? value = m[key].GetValue(source);
                if (key == EntityKeyKind.PartitionKey.ToString() || key == EntityKeyKind.RowKey.ToString())
                {
                    if (value != null)
                    {
                        value = value is DateTimeOffset timeValue ? DateTimeOffsetExtensions.ToReverseUnixTimestamp(timeValue) : (value?.ToString());
                    }
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

        public static T FromTableEntity<T>(this IDictionary<string, object?> source) where T : class, new()
        {
            return typeof(T) == typeof(Dictionary<string, object?>)
                ? (T)(object)new Dictionary<string, object?>(source)
                : source.ToObject<T>(AzureTableEntityMapping);
        }

        private static T ToObject<T>(this IDictionary<string, object?> source, Dictionary<string, string> specialMapping)
            where T : class, new()
        {
            T obj = new();
            Type type = obj.GetType();
            IReadOnlyDictionary<string, PropertyInfo> mapping = EntityMappingHelper.GetMapping(type);

            foreach (KeyValuePair<string, object?> item in source)
            {
                string keyName = specialMapping.TryGetValue(item.Key, out string? mappedKey) ? mappedKey : item.Key;
                if (mapping.TryGetValue(keyName, out PropertyInfo? value))
                {
                    object? itemValue = item.Value;
                    if (keyName == EntityKeyKind.Timestamp.ToString() && itemValue is long epoch)
                    {
                        itemValue = epoch < 9999999999 ? DateTimeOffset.FromUnixTimeSeconds(epoch) : DateTimeOffset.FromUnixTimeMilliseconds(epoch);
                    }

                    if ((keyName == EntityKeyKind.PartitionKey.ToString() || keyName == EntityKeyKind.RowKey.ToString())
                        && value.PropertyType != typeof(string))
                    {
                        if (itemValue != null)
                        {
                            if (value.PropertyType == typeof(DateTimeOffset))
                            {
                                long reverseTimestamp = long.Parse((string)itemValue);
                                itemValue = DateTimeOffsetExtensions.FromReverseUnixTimeMilliseconds(reverseTimestamp);
                            }
                            else
                            {
                                itemValue = TypeConverter.Convert((string)itemValue, value.PropertyType);
                            }
                        }
                    }

                    value.SetValue(obj, itemValue);
                }
            }

            return obj;
        }
    }
}
