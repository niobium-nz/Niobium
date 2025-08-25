namespace Niobium
{
    public static class IReadOnlyDictionaryExtensions
    {
        public static bool TryGetValueAsString(this IReadOnlyDictionary<string, object> dic, string key, out string value)
        {
            value = string.Empty;

            if (!dic.ContainsKey(key))
            {
                return false;
            }
            object v = dic[key];
            if (v is string v3)
            {
                value = v3;
                return true;
            }

            return false;
        }

        public static string GetValueAsString(this IReadOnlyDictionary<string, object> dic, string key)
        {
            return !dic.TryGetValueAsString(key, out string v) ? throw new InvalidCastException() : v;
        }

        public static bool TryGetValueAsBoolean(this IReadOnlyDictionary<string, object> dic, string key, out bool value)
        {
            value = false;

            if (!dic.ContainsKey(key))
            {
                return false;
            }
            object v = dic[key];
            if (v is bool v2)
            {
                value = v2;
                return true;
            }
            else if (v is string v3)
            {
                value = bool.Parse(v3);
                return true;
            }

            return false;
        }

        public static bool GetValueAsBoolean(this IReadOnlyDictionary<string, object> dic, string key)
        {
            return !dic.TryGetValueAsBoolean(key, out bool v) ? throw new InvalidCastException() : v;
        }

        public static bool TryGetValueAsInt32(this IReadOnlyDictionary<string, object> dic, string key, out int value)
        {
            value = 0;

            if (!dic.ContainsKey(key))
            {
                return false;
            }
            object v = dic[key];
            if (v is int v2)
            {
                value = v2;
                return true;
            }
            else if (v is string v3)
            {
                value = int.Parse(v3);
                return true;
            }

            return false;
        }

        public static int GetValueAsInt32(this IReadOnlyDictionary<string, object> dic, string key)
        {
            return !dic.TryGetValueAsInt32(key, out int v) ? throw new InvalidCastException() : v;
        }

        public static bool TryGetValueAsInt64(this IReadOnlyDictionary<string, object> dic, string key, out long value)
        {
            value = 0;

            if (!dic.ContainsKey(key))
            {
                return false;
            }
            object v = dic[key];
            if (v is long v2)
            {
                value = v2;
                return true;
            }
            else if (v is string v3)
            {
                value = long.Parse(v3);
                return true;
            }

            return false;
        }

        public static long GetValueAsInt64(this IReadOnlyDictionary<string, object> dic, string key)
        {
            return !dic.TryGetValueAsInt64(key, out long v) ? throw new InvalidCastException() : v;
        }
    }
}
