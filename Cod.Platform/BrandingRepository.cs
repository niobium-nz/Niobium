using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Cod.Platform
{
    internal class BrandingRepository : IRepository<BrandingInfo>
    {
        private static readonly string[] keys = new[] { "DEVELOP", "BEICHAO", "NIAOJU" };
        private static Dictionary<string, BrandingInfo> cache;

        private readonly Lazy<IConfigurationProvider> configuration;

        public BrandingRepository(Lazy<IConfigurationProvider> configuration) => this.configuration = configuration;

        public async Task<TableQueryResult<BrandingInfo>> GetAsync(string partitionKey, int limit)
        {
            if (String.IsNullOrWhiteSpace(partitionKey))
            {
                throw new ArgumentNullException(nameof(partitionKey));
            }

            if (cache == null)
            {
                await this.LoadAsync();
            }

            partitionKey = partitionKey.Trim().ToUpperInvariant();
            if (cache.ContainsKey(partitionKey))
            {
                return new TableQueryResult<BrandingInfo>(new[] { cache[partitionKey] }, null);
            }

            return new TableQueryResult<BrandingInfo>(new BrandingInfo[0], null);
        }

        public async Task<TableQueryResult<BrandingInfo>> GetAsync(int limit)
        {
            if (cache == null)
            {
                await this.LoadAsync();
            }
            return new TableQueryResult<BrandingInfo>(cache.Values.ToList(), null);
        }

        private async Task LoadAsync()
        {
            cache = new Dictionary<string, BrandingInfo>();
            foreach (var item in keys)
            {
                var setting = await this.configuration.Value.GetSettingAsync(item);
                if (!string.IsNullOrEmpty(setting))
                {
                    var branding = JsonConvert.DeserializeObject<BrandingInfo>(setting);
                    cache.Add(item, branding);
                }
            }
        }

        public Task<IEnumerable<BrandingInfo>> CreateAsync(IEnumerable<BrandingInfo> entities, bool replaceIfExist) => throw new NotImplementedException();
        public Task<IEnumerable<BrandingInfo>> DeleteAsync(IEnumerable<BrandingInfo> entities) => throw new NotImplementedException();
        public Task<BrandingInfo> GetAsync(string partitionKey, string rowKey) => throw new NotImplementedException();
        public Task<IEnumerable<BrandingInfo>> UpdateAsync(IEnumerable<BrandingInfo> entities) => throw new NotImplementedException();
    }
}
