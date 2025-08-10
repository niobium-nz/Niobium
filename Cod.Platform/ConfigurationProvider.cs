using Microsoft.Extensions.Configuration;
using System.Collections.Concurrent;

namespace Cod.Platform
{
    public class ConfigurationProvider : IConfigurationProvider
    {
        private static Func<IConfigurationBuilder, IConfigurationBuilder>? CustomConfig;
        private static readonly ConcurrentDictionary<string, string> Caches = new();

        private static readonly Lazy<IConfiguration> config = new(
            () =>
            {
                IConfigurationBuilder builder = new ConfigurationBuilder();
                if (CustomConfig != null)
                {
                    builder = CustomConfig(builder);
                }
                IConfigurationRoot root = builder.AddEnvironmentVariables().Build();
                return root;
            }, LazyThreadSafetyMode.ExecutionAndPublication);

        public static IConfiguration Configuration => config.Value;

        public static void Configure(Func<IConfigurationBuilder, IConfigurationBuilder> func)
        {
            CustomConfig = func;
        }

        public Task<string?> GetSettingAsStringAsync(string key, bool cache = true)
        {
            if (cache)
            {
                if (Caches.TryGetValue(key, out string? value))
                {
                    return Task.FromResult<string?>(value);
                }
            }

            string? v = GetSetting(key);

            if (cache && v != null)
            {
                if (Caches.ContainsKey(key))
                {
                    Caches[key] = v;
                }
                else
                {
                    _ = Caches.AddOrUpdate(key, _ => v, (_, _) => v);
                }
            }
            return Task.FromResult<string?>(v);
        }

        public string? GetSettingAsString(string key, bool cache = true)
        {
            return GetSetting(key, cache);
        }

        public static string? GetSetting(string key, bool cache = true)
        {
            if (cache)
            {
                if (Caches.TryGetValue(key, out string? value))
                {
                    return value;
                }
            }

            string? v = config.Value[key];
            v ??= config.Value[$"Values:{key}"];

            if (cache && v != null)
            {
                if (Caches.ContainsKey(key))
                {
                    Caches[key] = v;
                }
                else
                {
                    _ = Caches.AddOrUpdate(key, _ => v, (_, _) => v);
                }
            }
            return v;
        }
    }
}
