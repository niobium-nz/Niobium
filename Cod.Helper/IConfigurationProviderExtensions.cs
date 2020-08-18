using System.Threading.Tasks;

namespace Cod
{
    public static class IConfigurationProviderExtensions
    {
        public static T GetSetting<T>(this IConfigurationProvider configuration, string key, bool cache = true)
        {
            var str = configuration.GetSettingAsString(key, cache);
            if (str == null)
            {
                return default;
            }
            return TypeConverter.Convert<T>(str);
        }

        public static async Task<T> GetSettingAsync<T>(this IConfigurationProvider configuration, string key, bool cache = true)
        {
            var str = await configuration.GetSettingAsStringAsync(key, cache);
            return TypeConverter.Convert<T>(str);
        }
    }
}
