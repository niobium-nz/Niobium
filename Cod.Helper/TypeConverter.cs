using System;
using System.ComponentModel;
using System.Linq;
using Newtonsoft.Json;

namespace Cod
{
    public static class TypeConverter
    {
        private static readonly Type[] ConvertibleTypes = new[] { typeof(Guid), typeof(DateTime), typeof(DateTimeOffset), typeof(TimeSpan), };

        public static T Convert<T>(string input)
        {
            T obj;
            var t = typeof(T);
            if (typeof(IConvertible).IsAssignableFrom(t) || ConvertibleTypes.Contains(t))
            {
                var converter = TypeDescriptor.GetConverter(t);
                obj = (T)converter.ConvertFrom(input);
            }
            else
            {
                obj = JsonConvert.DeserializeObject<T>(input);
            }

            return obj;
        }
    }
}
