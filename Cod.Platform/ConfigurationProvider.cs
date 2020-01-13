using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Cod.Platform
{
    public class ConfigurationProvider : IConfigurationProvider
    {
        private static bool isSecureVaultEnabled = false;
        private static Func<IConfigurationBuilder, IConfigurationBuilder> customConfig;
        private static readonly Dictionary<string, string> caches = new Dictionary<string, string>();

        private static readonly Lazy<IConfiguration> config = new Lazy<IConfiguration>(
            () =>
            {
                IConfigurationBuilder builder = new ConfigurationBuilder();
                if (customConfig != null)
                {
                    builder = customConfig(builder);
                }
                return builder.AddEnvironmentVariables().Build();
            }, LazyThreadSafetyMode.ExecutionAndPublication);

        public static void Configure(Func<IConfigurationBuilder, IConfigurationBuilder> func, bool secureVaultEnabled)
        {
            customConfig = func;
            isSecureVaultEnabled = secureVaultEnabled;
        }

        public async Task<string> GetSettingAsync(string key, bool cache = true)
        {
            if (cache)
            {
                if (caches.ContainsKey(key))
                {
                    return caches[key];
                }
            }

            var v = GetSetting(key);
            if (isSecureVaultEnabled && v == null)
            {
                v = await SecureVault.GetSecretAsync(key);
            }

            if (cache)
            {
                if (caches.ContainsKey(key))
                {
                    caches[key] = v;
                }
                else
                {
                    caches.Add(key, v);
                }
            }
            return v;
        }

        public static string GetSetting(string key)
        {
            var v = config.Value[key];
            if (v == null)
            {
                v = config.Value[$"Values:{key}"];
            }
            return v;
        }
    }
}
