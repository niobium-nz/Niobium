using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Cod.Platform
{
    public class ConfigurationProvider : IConfigurationProvider
    {
        private static string KeyVaultUrl;
        private static Func<IConfigurationBuilder, IConfigurationBuilder> CustomConfig;
        private static readonly Dictionary<string, string> Caches = new Dictionary<string, string>();

        private static readonly Lazy<IConfiguration> config = new Lazy<IConfiguration>(
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

        public static void Configure(Func<IConfigurationBuilder, IConfigurationBuilder> func, string keyVaultUrl)
        {
            CustomConfig = func;
            KeyVaultUrl = keyVaultUrl;
        }

        public async Task<string> GetSettingAsStringAsync(string key, bool cache = true)
        {
            var v = GetSetting(key);
            if (!String.IsNullOrWhiteSpace(KeyVaultUrl) && v == null && Uri.TryCreate(KeyVaultUrl, UriKind.Absolute, out var uri))
            {
                v = await SecureVault.GetSecretAsync(uri, key);
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
            if (v == null)
            {
                v = config.Value[$"Values:{key}"];
            }

            if (v == null && key == "STORAGE-ACCOUNT")
            {
                return GetSetting("STORAGE_ACCOUNT", cache);
            }

            if (cache && v != null)
            {
                if (Caches.ContainsKey(key))
                {
                    Caches[key] = v;
                }
                else
                {
                    Caches.Add(key, v);
                }
            }
            return v;
        }
    }
}
