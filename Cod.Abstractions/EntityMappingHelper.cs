using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.Serialization;

namespace Cod
{
    public static class EntityMappingHelper
    {
        private static readonly Dictionary<string, PropertyInfo> emptyMapping = new();
        private static readonly ConcurrentDictionary<Type, Dictionary<string, PropertyInfo>> mappings = new();
        private static readonly BindingFlags bindingAttr = BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance;
        private static readonly Dictionary<string, string> AzureTableEntityMapping = new()
        {
            { "odata.etag", EntityKeyKind.ETag.ToString() },
        };

        public static IReadOnlyDictionary<string, PropertyInfo> GetMapping(Type type)
        {
            return mappings.ContainsKey(type) ? mappings[type] : emptyMapping;
        }

        public static T GetField<T>(object source, EntityKeyKind field)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            Type type = source.GetType();
            Dictionary<string, PropertyInfo> m = mappings[type];
            string key = field.ToString();
            if (!m.ContainsKey(key))
            {
                throw new InvalidDataException($"Cannot retrieve '{key}' from '{type.FullName}'.");
            }

            object value = m[key].GetValue(source);
            return (T)value;
        }

        public static bool TryGetField<T>(object source, EntityKeyKind field, out T result)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            Type type = source.GetType();
            Dictionary<string, PropertyInfo> m = mappings[type];
            string key = field.ToString();
            if (!m.ContainsKey(key))
            {
                result = default;
                return false;
            }

            object value = m[key].GetValue(source);
            result = (T)value;
            return true;
        }

        public static IDictionary<string, object> ToCosmosEntity(object source, IDictionary<string, string> specialMapping)
        {
            Dictionary<string, object> dic = new();
            Type type = source.GetType();

            Dictionary<string, PropertyInfo> m = mappings[type];
            foreach (string key in m.Keys)
            {
                object value = m[key].GetValue(source);
                if (key == EntityKeyKind.Timestamp.ToString() && value is DateTimeOffset time)
                {
                    value = time.ToUnixTimeSeconds();
                }

                if (specialMapping.TryGetValue(key, out string mappedKey))
                {
                    dic.Add(mappedKey, value);
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

        private static T ToObject<T>(this IDictionary<string, object> source, IDictionary<string, string> specialMapping)
            where T : class, new()
        {
            T obj = new();
            Type type = obj.GetType();

            if (!mappings.ContainsKey(type))
            {
                Dictionary<string, PropertyInfo> mapping = new();
                PropertyInfo[] properties = type.GetProperties(bindingAttr);
                foreach (PropertyInfo property in properties)
                {
                    EntityKeyAttribute key = property.GetCustomAttribute<EntityKeyAttribute>();
                    if (key != null)
                    {
                        switch (key.Kind)
                        {
                            case EntityKeyKind.PartitionKey:
                            case EntityKeyKind.RowKey:
                            case EntityKeyKind.ETag:
                                if (property.PropertyType != typeof(string))
                                {
                                    throw new InvalidDataContractException($"{key.Kind} property '{property.Name}' on '{type.FullName}' must be decleared as string.");
                                }
                                break;
                            case EntityKeyKind.Timestamp:
                                if (property.PropertyType != typeof(DateTimeOffset) && property.PropertyType != typeof(DateTimeOffset?))
                                {
                                    throw new InvalidDataContractException($"{key.Kind} property '{property.Name}' on '{type.FullName}' must be decleared as DateTimeOffset.");
                                }
                                break;
                        }

                        mapping.Add(key.Kind.ToString(), property);
                    }
                    else
                    {
                        mapping.Add(property.Name, property);
                    }
                }

                if (!mapping.ContainsKey(EntityKeyKind.PartitionKey.ToString()))
                {
                    throw new InvalidDataContractException($"'{type.FullName}' must decleare a property marked as '{nameof(EntityKeyAttribute)}({nameof(EntityKeyAttribute.Kind)}={EntityKeyKind.PartitionKey})'.");
                }
                if (!mapping.ContainsKey(EntityKeyKind.RowKey.ToString()))
                {
                    throw new InvalidDataContractException($"'{type.FullName}' must decleare a property marked as '{nameof(EntityKeyAttribute)}({nameof(EntityKeyAttribute.Kind)}={EntityKeyKind.RowKey})'.");
                }

                mappings.AddOrUpdate(type, mapping, (key, value) => value);
            }

            Dictionary<string, PropertyInfo> m = mappings[type];
            foreach (KeyValuePair<string, object> item in source)
            {
                string keyName = specialMapping.TryGetValue(item.Key, out string mappedKey) ? mappedKey : item.Key;
                if (m.TryGetValue(keyName, out PropertyInfo value))
                {
                    object itemValue = item.Value;
                    if (keyName == EntityKeyKind.Timestamp.ToString() && itemValue is long epoch)
                    {
                        itemValue = epoch < 9999999999 ? DateTimeOffset.FromUnixTimeSeconds(epoch) : DateTimeOffset.FromUnixTimeMilliseconds(epoch);
                    }

                    value.SetValue(obj, itemValue);
                }
            }

            return obj;
        }

    }
}
