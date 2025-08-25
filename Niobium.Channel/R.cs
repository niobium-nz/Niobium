using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Niobium.Channel
{
    internal static class R
    {
        public static bool TryGet(string key, [NotNullWhen(true)] out string? value)
        {
            string? str = Localization.ResourceManager.GetString(key, CultureInfo.CurrentUICulture);
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
            string? str = Localization.ResourceManager.GetString(key, CultureInfo.CurrentUICulture);
            return str ?? key;
        }
    }
}
