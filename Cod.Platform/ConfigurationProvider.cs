using Microsoft.Extensions.Configuration;
using System.Collections.Concurrent;

namespace Cod.Platform
{
    public class ConfigurationProvider : IConfigurationProvider
    {
        private static string? KeyVaultUrl;
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

        public static void EnableKeyValueSupport(string keyVaultUrl)
        {
            if (string.IsNullOrWhiteSpace(keyVaultUrl))
            {
                throw new ArgumentException($"'{nameof(keyVaultUrl)}' cannot be null or whitespace.", nameof(keyVaultUrl));
            }

            KeyVaultUrl = keyVaultUrl.EndsWith('/') ? keyVaultUrl[..^1] : keyVaultUrl;
        }

        public async Task<string?> GetSettingAsStringAsync(string key, bool cache = true)
        {
            if (cache)
            {
                if (Caches.TryGetValue(key, out string? value))
                {
                    return value;
                }
            }

            var v = GetSetting(key);
            if (!string.IsNullOrWhiteSpace(KeyVaultUrl) && v == null && Uri.TryCreate(KeyVaultUrl, UriKind.Absolute, out var uri))
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
