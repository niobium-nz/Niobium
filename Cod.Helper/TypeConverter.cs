using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Cod
{
    public static class TypeConverter
    {
        private static readonly Type[] ConvertibleTypes = new[] { typeof(Guid), typeof(DateTime), typeof(DateTimeOffset), typeof(TimeSpan), };

        public static T Convert<T>(string input)
        {
            return (T)Convert(input, typeof(T));
        }

        public static object Convert(string input, Type targetType)
        {
            object obj;
            if (targetType != typeof(string) && typeof(IEnumerable).IsAssignableFrom(targetType))
            {
                Type[] genericArgs = targetType.GetGenericArguments();
                if (genericArgs.Length != 1)
                {
                    throw new NotSupportedException();
                }

                string[] parts = input.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                IList result = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(genericArgs));
                foreach (string part in parts)
                {
                    obj = Convert(part, genericArgs[0]);
                    result.Add(obj);
                }
                return result;
            }
            else if (typeof(IConvertible).IsAssignableFrom(targetType) || ConvertibleTypes.Contains(targetType))
            {
                System.ComponentModel.TypeConverter converter = TypeDescriptor.GetConverter(targetType);
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
