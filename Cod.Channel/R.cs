using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Cod.Channel
{
    internal static class R
    {
        public static bool TryGet(string key, [NotNullWhen(true)] out string? value)
        {
            var str = Localization.ResourceManager.GetString(key, CultureInfo.CurrentUICulture);
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
            var str = Localization.ResourceManager.GetString(key, CultureInfo.CurrentUICulture);
            return str ?? key;
        }
    }
}
