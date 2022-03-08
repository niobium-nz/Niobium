using System;
using System.Collections.Generic;

namespace Cod
{
    public static class IReadOnlyDictionaryExtensions
    {
        public static bool TryGetValueAsString(this IReadOnlyDictionary<string, object> dic, string key, out string value)
        {
            value = String.Empty;

            if (!dic.ContainsKey(key))
            {
                return false;
            }
            var v = dic[key];
            if (v is string v3)
            {
                value = v3;
                return true;
            }

            return false;
        }

        public static string GetValueAsString(this IReadOnlyDictionary<string, object> dic, string key)
        {
            if (!TryGetValueAsString(dic, key, out var v))
            {
                throw new InvalidCastException();
            }
            return v;
        }

        public static bool TryGetValueAsBoolean(this IReadOnlyDictionary<string, object> dic, string key, out bool value)
        {
            value = false;

            if (!dic.ContainsKey(key))
            {
                return false;
            }
            var v = dic[key];
            if (v is bool v2)
            {
                value = v2;
                return true;
            }
            else if (v is string v3)
            {
                value = Boolean.Parse(v3);
                return true;
            }

            return false;
        }

        public static bool GetValueAsBoolean(this IReadOnlyDictionary<string, object> dic, string key)
        {
            if (!TryGetValueAsBoolean(dic, key, out var v))
            {
                throw new InvalidCastException();
            }
            return v;
        }

        public static bool TryGetValueAsInt32(this IReadOnlyDictionary<string, object> dic, string key, out int value)
        {
            value = 0;

            if (!dic.ContainsKey(key))
            {
                return false;
            }
            var v = dic[key];
            if (v is int v2)
            {
                value = v2;
                return true;
            }
            else if (v is string v3)
            {
                value = Int32.Parse(v3);
                return true;
            }

            return false;
        }

        public static int GetValueAsInt32(this IReadOnlyDictionary<string, object> dic, string key)
        {
            if (!TryGetValueAsInt32(dic, key, out var v))
            {
                throw new InvalidCastException();
            }
            return v;
        }

        public static bool TryGetValueAsInt64(this IReadOnlyDictionary<string, object> dic, string key, out long value)
        {
            value = 0;

            if (!dic.ContainsKey(key))
            {
                return false;
            }
            var v = dic[key];
            if (v is long v2)
            {
                value = v2;
                return true;
            }
            else if (v is string v3)
            {
                value = Int64.Parse(v3);
                return true;
            }

            return false;
        }

        public static long GetValueAsInt64(this IReadOnlyDictionary<string, object> dic, string key)
        {
            if (!TryGetValueAsInt64(dic, key, out var v))
            {
                throw new InvalidCastException();
            }
            return v;
        }
    }
}
