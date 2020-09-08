using System.Collections.Generic;

namespace Cod
{
    internal static class R
    {
        public static bool TryGet(string key, out string value)
        {
            var culture = UICulture.Current;
            var str = Localization.ResourceManager.GetString(key, culture);
            if (str != null)
            {
                value = str;
                return true;
            }

            value = default;
            return false;
        }

        public static string Get(string key)
        {
            if (!TryGet(key, out var val))
            {
                throw new KeyNotFoundException();
            }

            return val;
        }
    }
}
