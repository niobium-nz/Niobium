using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Cod.Platform
{
    internal class ConfigurationProvider : Cod.IConfigurationProvider
    {
        private static readonly Dictionary<string, string> caches = new Dictionary<string, string>();

        private static readonly Lazy<IConfiguration> config = new Lazy<IConfiguration>(
            () => new ConfigurationBuilder()
#if DEBUG
                .SetBasePath(new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
#endif
                .AddEnvironmentVariables()
                .Build(), LazyThreadSafetyMode.ExecutionAndPublication);

        public async Task<string> GetSettingAsync(string key, bool cache = true)
        {
            if (cache)
            {
                if (caches.ContainsKey(key))
                {
                    return caches[key];
                }
            }

            var v = config.Value[key];
#if DEBUG
            if (v == null)
            {
                v = config.Value[$"Values:{key}"];
            }
#endif
            if (v == null)
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
#if DEBUG
            var v = config.Value[$"Values:{key}"];
            return v ?? config.Value[key];
#else
            return config.Value[key];
#endif
        }
    }
}
