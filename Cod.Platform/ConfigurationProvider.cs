using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;

namespace Cod.Platform
{
    public class ConfigurationProvider : IConfigurationProvider
    {
        private static string KeyVaultUrl;
        private static Func<IConfigurationBuilder, IConfigurationBuilder> CustomConfig;
        private static readonly ConcurrentDictionary<string, string> Caches = new();

        private static readonly Lazy<IConfiguration> config = new(
            () =>
            {
                IConfigurationBuilder builder = new ConfigurationBuilder();
                if (CustomConfig != null)
                {
                    builder = CustomConfig(builder);
                }
                var root = builder.AddEnvironmentVariables().Build();
                return root;
            }, LazyThreadSafetyMode.ExecutionAndPublication);

        public static IConfiguration Configuration => config.Value;

        public static void Configure(Func<IConfigurationBuilder, IConfigurationBuilder> func) => CustomConfig = func;

        public static void EnableKeyValueSupport(string keyVaultUrl) => KeyVaultUrl = keyVaultUrl;

        public async Task<string> GetSettingAsStringAsync(string key, bool cache = true)
        {
            if (cache)
            {
                if (Caches.ContainsKey(key))
                {
                    return Caches[key];
                }
            }

            var v = GetSetting(key);
            if (!String.IsNullOrWhiteSpace(KeyVaultUrl) && v == null && Uri.TryCreate(KeyVaultUrl, UriKind.Absolute, out var uri))
            {
                v = await SecureVault.GetSecretAsync(uri, key);
            }

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

        public string GetSettingAsString(string key, bool cache = true) => GetSetting(key, cache);

        public static string GetSetting(string key, bool cache = true)
        {
            if (cache)
            {
                if (Caches.ContainsKey(key))
                {
                    return Caches[key];
                }
            }

            var v = config.Value[key];
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
