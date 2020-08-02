using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Azure.Cosmos.Table;

namespace Cod.Platform.Model
{
    public static class TableEntityHelper
    {
        private static ConcurrentDictionary<Type, Dictionary<string, EdmType>> propertyResolverCache = new ConcurrentDictionary<Type, Dictionary<string, EdmType>>();

        private static bool disablePropertyResolverCache = false;

        internal static ConcurrentDictionary<Type, Dictionary<string, EdmType>> PropertyResolverCache
        {
            get => propertyResolverCache;
            set => propertyResolverCache = value;
        }

        internal static bool DisablePropertyResolverCache
        {
            get => disablePropertyResolverCache;
            set
            {
                if (value)
                {
                    propertyResolverCache.Clear();
                }
                disablePropertyResolverCache = value;
            }
        }

        internal static void ReadUserObject(object entity, IDictionary<string, EntityProperty> properties, OperationContext operationContext)
        {
            if (entity is null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            ReflectionRead(entity, properties, operationContext);
        }

        internal static TResult ConvertBack<TResult>(IDictionary<string, EntityProperty> properties, OperationContext operationContext) => EntityPropertyConverter.ConvertBack<TResult>(properties, operationContext);

        internal static TResult ConvertBack<TResult>(IDictionary<string, EntityProperty> properties, EntityPropertyConverterOptions entityPropertyConverterOptions, OperationContext operationContext) => EntityPropertyConverter.ConvertBack<TResult>(properties, entityPropertyConverterOptions, operationContext);

        public static void ReflectionRead(object entity, IDictionary<string, EntityProperty> properties, OperationContext operationContext)
        {
            foreach (var item in (IEnumerable<PropertyInfo>)entity.GetType().GetProperties())
            {
                if (!ShouldSkipProperty(item, operationContext))
                {
                    if (!properties.ContainsKey(item.Name))
                    {
                        //Logger.LogInformational(operationContext, "Omitting property '{0}' from de-serialization because there is no corresponding entry in the dictionary provided.", item.Name);
                    }
                    else
                    {
                        var entityProperty = properties[item.Name];
                        if (entityProperty.PropertyAsObject == null)
                        {
                            item.SetValue(entity, null, null);
                        }
                        else
                        {
                            switch (entityProperty.PropertyType)
                            {
                                case EdmType.String:
                                    if (!(item.PropertyType != typeof(string)))
                                    {
                                        item.SetValue(entity, entityProperty.StringValue, null);
                                    }
                                    break;
                                case EdmType.Binary:
                                    if (!(item.PropertyType != typeof(byte[])))
                                    {
                                        item.SetValue(entity, entityProperty.BinaryValue, null);
                                    }
                                    break;
                                case EdmType.Boolean:
                                    if (!(item.PropertyType != typeof(bool)) || !(item.PropertyType != typeof(bool?)))
                                    {
                                        item.SetValue(entity, entityProperty.BooleanValue, null);
                                    }
                                    break;
                                case EdmType.DateTime:
                                    if (item.PropertyType == typeof(DateTime))
                                    {
                                        item.SetValue(entity, entityProperty.DateTimeOffsetValue.Value.UtcDateTime, null);
                                    }
                                    else if (item.PropertyType == typeof(DateTime?))
                                    {
                                        item.SetValue(entity, entityProperty.DateTimeOffsetValue.HasValue ? new DateTime?(entityProperty.DateTimeOffsetValue.Value.UtcDateTime) : null, null);
                                    }
                                    else if (item.PropertyType == typeof(DateTimeOffset))
                                    {
                                        item.SetValue(entity, entityProperty.DateTimeOffsetValue.Value, null);
                                    }
                                    else if (item.PropertyType == typeof(DateTimeOffset?))
                                    {
                                        item.SetValue(entity, entityProperty.DateTimeOffsetValue, null);
                                    }
                                    break;
                                case EdmType.Double:
                                    if (!(item.PropertyType != typeof(double)) || !(item.PropertyType != typeof(double?)))
                                    {
                                        item.SetValue(entity, entityProperty.DoubleValue, null);
                                    }
                                    break;
                                case EdmType.Guid:
                                    if (!(item.PropertyType != typeof(Guid)) || !(item.PropertyType != typeof(Guid?)))
                                    {
                                        item.SetValue(entity, entityProperty.GuidValue, null);
                                    }
                                    break;
                                case EdmType.Int32:
                                    if (!(item.PropertyType != typeof(int)) || !(item.PropertyType != typeof(int?)))
                                    {
                                        item.SetValue(entity, entityProperty.Int32Value, null);
                                    }
                                    break;
                                case EdmType.Int64:
                                    if (!(item.PropertyType != typeof(long)) || !(item.PropertyType != typeof(long?)))
                                    {
                                        item.SetValue(entity, entityProperty.Int64Value, null);
                                    }
                                    break;
                            }
                        }
                    }
                }
            }
        }

        internal static IDictionary<string, EntityProperty> WriteUserObject(object entity, OperationContext operationContext)
        {
            if (entity is null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            return ReflectionWrite(entity, operationContext);
        }

        internal static IDictionary<string, EntityProperty> Flatten(object entity, OperationContext operationContext)
        {
            if (entity is null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            return EntityPropertyConverter.Flatten(entity, operationContext);
        }

        internal static IDictionary<string, EntityProperty> Flatten(object entity, EntityPropertyConverterOptions entityPropertyConverterOptions, OperationContext operationContext)
        {
            if (entity is null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            return EntityPropertyConverter.Flatten(entity, entityPropertyConverterOptions, operationContext);
        }

        public static IDictionary<string, EntityProperty> ReflectionWrite(object entity, OperationContext operationContext)
        {
            var dictionary = new Dictionary<string, EntityProperty>();
            foreach (var item in (IEnumerable<PropertyInfo>)entity.GetType().GetProperties())
            {
                if (!ShouldSkipProperty(item, operationContext))
                {
                    var entityProperty = CreateEntityPropertyFromObject(item.GetValue(entity, null), item.PropertyType);
                    if (entityProperty != null)
                    {
                        dictionary.Add(item.Name, entityProperty);
                    }
                }
            }
            return dictionary;
        }

        internal static bool ShouldSkipProperty(PropertyInfo property, OperationContext operationContext)
        {
            switch (property.Name)
            {
                case "PartitionKey":
                case "RowKey":
                case "Timestamp":
                case "ETag":
                    return true;
                default:
                    {
                        var setMethod = property.SetMethod;
                        var getMethod = property.GetMethod;
                        if (setMethod == null || !setMethod.IsPublic || getMethod == null || !getMethod.IsPublic)
                        {
                            //Logger.LogInformational(operationContext, "Omitting property '{0}' from serialization/de-serialization because the property's getter/setter are not public.", property.Name);
                            return true;
                        }
                        if (setMethod.IsStatic)
                        {
                            return true;
                        }
                        if (Attribute.IsDefined(property, typeof(IgnorePropertyAttribute)))
                        {
                            //Logger.LogInformational(operationContext, "Omitting property '{0}' from serialization/de-serialization because IgnoreAttribute has been set on that property.", property.Name);
                            return true;
                        }
                        return false;
                    }
            }
        }

        private static EntityProperty CreateEntityPropertyFromObject(object value, Type type)
        {
            if (type == typeof(string))
            {
                return new EntityProperty((string)value);
            }
            if (type == typeof(byte[]))
            {
                return new EntityProperty((byte[])value);
            }
            if (type == typeof(bool))
            {
                return new EntityProperty((bool)value);
            }
            if (type == typeof(bool?))
            {
                return new EntityProperty((bool?)value);
            }
            if (type == typeof(DateTime))
            {
                return new EntityProperty((DateTime)value);
            }
            if (type == typeof(DateTime?))
            {
                return new EntityProperty((DateTime?)value);
            }
            if (type == typeof(DateTimeOffset))
            {
                return new EntityProperty((DateTimeOffset)value);
            }
            if (type == typeof(DateTimeOffset?))
            {
                return new EntityProperty((DateTimeOffset?)value);
            }
            if (type == typeof(double))
            {
                return new EntityProperty((double)value);
            }
            if (type == typeof(double?))
            {
                return new EntityProperty((double?)value);
            }
            if (type == typeof(Guid?))
            {
                return new EntityProperty((Guid?)value);
            }
            if (type == typeof(Guid))
            {
                return new EntityProperty((Guid)value);
            }
            if (type == typeof(int))
            {
                return new EntityProperty((int)value);
            }
            if (type == typeof(int?))
            {
                return new EntityProperty((int?)value);
            }
            if (type == typeof(long))
            {
                return new EntityProperty((long)value);
            }
            if (type == typeof(long?))
            {
                return new EntityProperty((long?)value);
            }
            return null;
        }
    }
}
