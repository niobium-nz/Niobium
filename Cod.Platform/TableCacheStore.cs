using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cod.Platform.Model;

namespace Cod.Platform
{
    internal class TableCacheStore : ICacheStore
    {
        private readonly Dictionary<string, object> memoryCache = new Dictionary<string, object>();
        private readonly Dictionary<string, DateTimeOffset> memoryCacheExpiry = new Dictionary<string, DateTimeOffset>();

        public async Task DeleteAsync(string partitionKey, string rowKey)
        {
            var memkey = $"{partitionKey}@{rowKey}";
            if (this.memoryCache.ContainsKey(memkey))
            {
                this.memoryCache.Remove(memkey);
            }
            var cache = await CloudStorage.GetTable<Cache>().RetrieveAsync<Cache>(partitionKey, rowKey);
            if (cache != null)
            {
                await CloudStorage.GetTable<Cache>().RemoveAsync(new[] { cache });
            }
        }

        public async Task<T> GetAsync<T>(string partitionKey, string rowKey) where T : IConvertible
        {
            if (String.IsNullOrWhiteSpace(partitionKey) || String.IsNullOrWhiteSpace(rowKey))
            {
                return default;
            }
            partitionKey = partitionKey.Trim();
            rowKey = rowKey.Trim();
            var memkey = $"{partitionKey}@{rowKey}";
            if (this.memoryCache.ContainsKey(memkey))
            {
                if (this.memoryCacheExpiry.ContainsKey(memkey) && this.memoryCacheExpiry[memkey] < DateTimeOffset.UtcNow)
                {
                    this.memoryCache.Remove(memkey);
                    this.memoryCacheExpiry.Remove(memkey);
                    var expiredcache = await CloudStorage.GetTable<Cache>().RetrieveAsync<Cache>(partitionKey, rowKey);
                    await CloudStorage.GetTable<Cache>().RemoveAsync(new[] { expiredcache });
                    return default;
                }
                return (T)this.memoryCache[memkey];
            }
            var cache = await CloudStorage.GetTable<Cache>().RetrieveAsync<Cache>(partitionKey, rowKey);
            if (cache != null)
            {
                if (cache.Expiry < DateTimeOffset.UtcNow)
                {
                    await CloudStorage.GetTable<Cache>().RemoveAsync(new[] { cache });
                    return default;
                }
                else
                {
                    this.memoryCache.Add(memkey, cache.Value);
                    this.memoryCacheExpiry.Add(memkey, cache.Expiry);
                    return (T)Convert.ChangeType(cache.Value, typeof(T));
                }
            }
            return default;
        }

        public async Task SetAsync<T>(string partitionKey, string rowKey, T value, DateTimeOffset? expiry = null) where T : IConvertible
        {
            if (String.IsNullOrWhiteSpace(partitionKey) || String.IsNullOrWhiteSpace(rowKey))
            {
                return;
            }
            partitionKey = partitionKey.Trim();
            rowKey = rowKey.Trim();
            var memkey = $"{partitionKey}@{rowKey}";
            if (this.memoryCache.ContainsKey(memkey))
            {
                this.memoryCache[memkey] = value;
            }
            else
            {
                this.memoryCache.Add(memkey, value);
            }
            if (expiry.HasValue)
            {
                if (this.memoryCacheExpiry.ContainsKey(memkey))
                {
                    this.memoryCacheExpiry[memkey] = expiry.Value;
                }
                else
                {
                    this.memoryCacheExpiry.Add(memkey, expiry.Value);
                }
            }
            await CloudStorage.GetTable<Cache>().InsertAsync(new[] { new Cache
            {
                PartitionKey = partitionKey,
                RowKey = rowKey,
                Value = value.ToString(),
                Expiry = expiry ?? DateTimeOffset.Parse("2100-01-01T00:00:00Z")
            } });
        }
    }
}

