using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Newtonsoft.Json;

namespace Cod
{
    public static class TypeConverter
    {
        private static readonly Type[] ConvertibleTypes = new[] { typeof(Guid), typeof(DateTime), typeof(DateTimeOffset), typeof(TimeSpan), };
        
        public static T Convert<T>(string input) => (T)Convert(input, typeof(T));

        public static object Convert(string input, Type targetType)
        {
            object obj;
            if (typeof(IEnumerable).IsAssignableFrom(targetType))
            {
                var genericArgs = targetType.GetGenericArguments();
                if (genericArgs.Length != 1)
                {
                    throw new NotSupportedException();
                }

                var parts = input.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                var result = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(genericArgs));
                foreach (var part in parts)
                {
                    obj = Convert(part, genericArgs[0]);
                    result.Add(obj);
                }
                return result;
            }
            else if (typeof(IConvertible).IsAssignableFrom(targetType) || ConvertibleTypes.Contains(targetType))
            {
                var converter = TypeDescriptor.GetConverter(targetType);
                obj = converter.ConvertFrom(input);
            }
            else
            {
                obj = JsonConvert.DeserializeObject(input, targetType);
            }

            return obj;
        }
    }
}
