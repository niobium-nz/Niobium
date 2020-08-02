using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Cod.Platform.Model;

namespace Cod.Platform
{
    public class TableCacheStore : ICacheStore
    {
        private readonly ConcurrentDictionary<string, object> memoryCache = new ConcurrentDictionary<string, object>();
        private readonly ConcurrentDictionary<string, DateTimeOffset> memoryCacheExpiry = new ConcurrentDictionary<string, DateTimeOffset>();

        public async Task DeleteAsync(string partitionKey, string rowKey)
        {
            var memkey = $"{partitionKey}@{rowKey}";
            this.memoryCache.TryRemove(memkey, out _);
            this.memoryCacheExpiry.TryRemove(memkey, out _);

            var cache = await CloudStorage.GetTable<Cache>().RetrieveAsync<Cache>(partitionKey, rowKey);
            if (cache != null)
            {
                await CloudStorage.GetTable<Cache>().RemoveAsync(new[] { cache }, successIfNotExist: true);
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
                if (this.memoryCacheExpiry.TryGetValue(memkey, out var value) && value < DateTimeOffset.UtcNow)
                {
                    this.memoryCache.TryRemove(memkey, out _);
                    this.memoryCacheExpiry.TryRemove(memkey, out _);
                    var expiredcache = await CloudStorage.GetTable<Cache>().RetrieveAsync<Cache>(partitionKey, rowKey);
                    if (expiredcache != null)
                    {
                        await CloudStorage.GetTable<Cache>().RemoveAsync(new[] { expiredcache }, true);
                    }
                    return default;
                }
                return (T)this.memoryCache[memkey];
            }
            var cache = await CloudStorage.GetTable<Cache>().RetrieveAsync<Cache>(partitionKey, rowKey);
            if (cache != null)
            {
                if (cache.Expiry < DateTimeOffset.UtcNow)
                {
                    await CloudStorage.GetTable<Cache>().RemoveAsync(new[] { cache }, true);
                    return default;
                }
                else
                {
                    if (cache.InMemory)
                    {
                        this.memoryCache.AddOrUpdate(memkey, cache.Value, (a, b) => cache.Value);
                        this.memoryCacheExpiry.AddOrUpdate(memkey, cache.Expiry, (a, b) => cache.Expiry);
                    }

                    return (T)Convert.ChangeType(cache.Value, typeof(T));
                }
            }
            return default;
        }

        public async Task SetAsync<T>(string partitionKey, string rowKey, T value, bool memoryCached, DateTimeOffset? expiry = null) where T : IConvertible
        {
            if (String.IsNullOrWhiteSpace(partitionKey) || String.IsNullOrWhiteSpace(rowKey))
            {
                return;
            }
            partitionKey = partitionKey.Trim();
            rowKey = rowKey.Trim();
            var memkey = $"{partitionKey}@{rowKey}";

            if (memoryCached)
            {
                this.memoryCache.AddOrUpdate(memkey, value, (a, b) => value);
                if (expiry.HasValue)
                {
                    this.memoryCacheExpiry.AddOrUpdate(memkey, expiry.Value, (a, b) => expiry.Value);
                }
            }

            await CloudStorage.GetTable<Cache>().InsertOrReplaceAsync(new[] { new Cache
            {
                PartitionKey = partitionKey,
                RowKey = rowKey,
                Value = value.ToString(),
                InMemory = memoryCached,
                Expiry = expiry ?? DateTimeOffset.Parse("2100-01-01T00:00:00Z")
            } });
        }
    }
}

