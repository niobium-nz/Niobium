using System.Reflection;

namespace Cod.Storage.Table
{
    internal class DBEntityHelper
    {
        public static EntityDictionary ToTableEntity(object source)
        {
            EntityDictionary dic = new();
            Type type = source.GetType();

            IReadOnlyDictionary<string, PropertyInfo> m = EntityMappingHelper.GetMapping(type);
            foreach (string key in m.Keys)
            {
                object value = m[key].GetValue(source);
                dic.Add(key, value);
            }

            return dic;
        }
    }
}
