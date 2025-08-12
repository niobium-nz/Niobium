using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Serialization;

namespace Cod
{
    public static class EntityMappingHelper
    {
        private static readonly ConcurrentDictionary<Type, Dictionary<string, PropertyInfo>> mappings = new();
        private static readonly BindingFlags bindingAttr = BindingFlags.Public | BindingFlags.Instance;

        public static IReadOnlyDictionary<string, PropertyInfo> GetMapping(Type type)
        {
            if (!mappings.ContainsKey(type))
            {
                BuildPropertyMapping(type);
            }

            return mappings[type];
        }

        public static T GetField<T>(object source, EntityKeyKind field)
        {
            return TryGetField<T>(source, field, out T? result) ? result : throw new InvalidDataException($"Cannot get {field} from {source}.");
        }

        public static bool TryGetField<T>(object source, EntityKeyKind field, [NotNullWhen(true)] out T? result)
        {
            Type type = source.GetType();
            IReadOnlyDictionary<string, PropertyInfo> mapping = GetMapping(type);
            string key = field.ToString();
            if (!mapping.ContainsKey(key))
            {
                result = default;
                return false;
            }

            object? value = mapping[key].GetValue(source);
            if (value == null)
            {
                result = default;
                return false;
            }

            result = field switch
            {
                EntityKeyKind.PartitionKey or EntityKeyKind.RowKey or EntityKeyKind.Timestamp => value is DateTimeOffset time
                                        ? typeof(T) == typeof(DateTimeOffset)
                                            ? (T)value
                                            : typeof(T) == typeof(long)
                                                ? (T)(object)time.ToReverseUnixTimeMilliseconds()
                                                : typeof(T) == typeof(string)
                                                ? (T)(object)time.ToReverseUnixTimeMilliseconds().ToString()
                                                : throw new InvalidDataContractException($"Property '{key}' on '{type.FullName}' must be of type DateTimeOffset or long.")
                                        : (T)(object)value.ToString()!,
                EntityKeyKind.ETag => (T)(object)value.ToString()!,
                _ => throw new NotSupportedException($"Converting '{value}' to type '{typeof(T)}' is not supported."),
            };
            return true;
        }

        public static IDictionary<string, object> ToCosmosEntity(object source, IDictionary<string, string> specialMapping)
        {
            Dictionary<string, object> dic = [];
            Type type = source.GetType();
            IReadOnlyDictionary<string, PropertyInfo> mapping = GetMapping(type);

            foreach (string key in mapping.Keys)
            {
                object? value = mapping[key].GetValue(source);
                if (key == EntityKeyKind.Timestamp.ToString() && value is DateTimeOffset time)
                {
                    value = time.ToUnixTimeSeconds();
                }

                if (value != null)
                {
                    if (specialMapping.TryGetValue(key, out string? mappedKey))
                    {
                        dic.Add(mappedKey, value);
                    }
                    else
                    {
                        dic.Add(key, value);
                    }
                }
            }

            return dic;
        }

        private static void BuildPropertyMapping(Type type)
        {
            Dictionary<string, PropertyInfo> mapping = [];
            PropertyInfo[] properties = type.GetProperties(bindingAttr);
            foreach (PropertyInfo property in properties)
            {
                EntityKeyAttribute? key = property.GetCustomAttribute<EntityKeyAttribute>();
                if (key != null)
                {
                    switch (key.Kind)
                    {
                        case EntityKeyKind.ETag:
                            if (property.PropertyType != typeof(string))
                            {
                                throw new InvalidDataContractException($"{key.Kind} property '{property.Name}' on '{type.FullName}' must be decleared as string.");
                            }
                            break;
                        case EntityKeyKind.Timestamp:
                            if (property.PropertyType != typeof(DateTimeOffset) && property.PropertyType != typeof(DateTimeOffset?))
                            {
                                throw new InvalidDataContractException($"{key.Kind} property '{property.Name}' on '{type.FullName}' must be decleared as System.DateTimeOffset.");
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
    }
}
