using System.Collections.Concurrent;
using System.Globalization;

namespace Cod.Platform
{
    internal class DatabaseCacheStore(IRepository<Cache> cacheRepo) : ICacheStore
    {
        private readonly ConcurrentDictionary<string, object> memoryCache = new();
        private readonly ConcurrentDictionary<string, DateTimeOffset> memoryCacheExpiry = new();

        public bool SupportTTL { get; set; }

        public async Task DeleteAsync(string partitionKey, string rowKey, CancellationToken cancellationToken = default)
        {
            string memkey = $"{partitionKey}@{rowKey}";
            memoryCache.TryRemove(memkey, out _);
            memoryCacheExpiry.TryRemove(memkey, out _);

            Cache cache = await cacheRepo.RetrieveAsync(partitionKey, rowKey, cancellationToken: cancellationToken);
            if (cache != null)
            {
                await cacheRepo.DeleteAsync(cache, preconditionCheck: false, successIfNotExist: true, cancellationToken: cancellationToken);
            }
        }

        public async Task<T?> GetAsync<T>(string partitionKey, string rowKey, CancellationToken cancellationToken = default) where T : IConvertible
        {
            partitionKey = partitionKey.Trim();
            rowKey = rowKey.Trim();
            string memkey = $"{partitionKey}@{rowKey}";
            if (memoryCache.TryGetValue(memkey, out var value))
            {
                if (memoryCacheExpiry.TryGetValue(memkey, out DateTimeOffset t) && t < DateTimeOffset.UtcNow)
                {
                    memoryCache.TryRemove(memkey, out _);
                    memoryCacheExpiry.TryRemove(memkey, out _);
                    Cache expiredcache = await cacheRepo.RetrieveAsync(partitionKey, rowKey, cancellationToken: cancellationToken);
                    if (expiredcache != null)
                    {
                        await cacheRepo.DeleteAsync(expiredcache, preconditionCheck: false, successIfNotExist: true, cancellationToken: cancellationToken);
                    }
                    return default;
                }
                return (T)value;
            }

            Cache cache = await cacheRepo.RetrieveAsync(partitionKey, rowKey, cancellationToken: cancellationToken);
            if (cache != null)
            {
                if (cache.Expiry < DateTimeOffset.UtcNow)
                {
                    await cacheRepo.DeleteAsync(cache, preconditionCheck: false, successIfNotExist: true, cancellationToken: cancellationToken);
                    return default;
                }
                else
                {
                    if (cache.InMemory)
                    {
                        memoryCache.AddOrUpdate(memkey, cache.Value, (a, b) => cache.Value);
                        memoryCacheExpiry.AddOrUpdate(memkey, cache.Expiry, (a, b) => cache.Expiry);
                    }

                    return (T)Convert.ChangeType(cache.Value, typeof(T), CultureInfo.InvariantCulture);
                }
            }
            return default;
        }

        public async Task SetAsync<T>(string partitionKey, string rowKey, T value, bool memoryCached, DateTimeOffset? expiry = null, CancellationToken cancellationToken = default) where T : IConvertible
        {
            if (string.IsNullOrWhiteSpace(partitionKey) || string.IsNullOrWhiteSpace(rowKey))
            {
                return;
            }
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value), "Value cannot be null.");
            }

            partitionKey = partitionKey.Trim();
            rowKey = rowKey.Trim();
            string memkey = $"{partitionKey}@{rowKey}";

            if (memoryCached)
            {
                memoryCache.AddOrUpdate(memkey, value, (a, b) => value);
                if (expiry.HasValue)
                {
                    memoryCacheExpiry.AddOrUpdate(memkey, expiry.Value, (a, b) => expiry.Value);
                }
            }

            await cacheRepo.CreateAsync(new Cache
            {
                PartitionKey = partitionKey,
                RowKey = rowKey,
                Value = value.ToString()!,
                InMemory = memoryCached,
                Expiry = expiry ?? DateTimeOffset.Parse("2100-01-01T00:00:00Z", CultureInfo.InvariantCulture)
            },
            replaceIfExist: true,
            expiry: SupportTTL ? expiry ?? DateTimeOffset.UtcNow.AddMonths(1) : null,
            cancellationToken: cancellationToken);
        }
    }
}

