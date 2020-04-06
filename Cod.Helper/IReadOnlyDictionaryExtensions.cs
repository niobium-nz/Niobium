using System.Collections.Generic;

namespace Cod
{
    public static class IReadOnlyDictionaryExtensions
    {
        public static string GetString(this IReadOnlyDictionary<string, object> dic, string key) => dic.ContainsKey(key) ? dic[key] as string : null;
    }
}
