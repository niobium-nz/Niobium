using System;
using System.Threading.Tasks;

namespace Cod.Platform
{
    internal class RedisCacheStore : ICacheStore
    {
        public async Task DeleteAsync(string partitionKey, string rowKey)
        {
            var db = await RedisClient.GetDatabaseAsync();
            await db.HashDeleteAsync(partitionKey, rowKey);
        }

        public async Task<T> GetAsync<T>(string partitionKey, string rowKey) where T : IConvertible
        {
            var key = $"{partitionKey}|{rowKey}";
            var db = await RedisClient.GetDatabaseAsync();
            var result = await db.StringGetAsync(key);
            if (!result.HasValue || result.IsNullOrEmpty)
            {
                return default;
            }
            var str = (string)result;
            return (T)Convert.ChangeType(str, typeof(T));
        }

        public async Task SetAsync<T>(string partitionKey, string rowKey, T value, DateTimeOffset? expiry = null) where T : IConvertible
        {
            if (expiry.HasValue && DateTimeOffset.UtcNow > expiry.Value)
            {
                //REMARK (5he11) 已经失效就没必要存储
                return;
            }

            var db = await RedisClient.GetDatabaseAsync();
            var key = $"{partitionKey}|{rowKey}";
            await db.StringSetAsync(key, value.ToString());
            if (expiry.HasValue)
            {
                await db.KeyExpireAsync(key, expiry.Value - DateTimeOffset.UtcNow);
            }
        }
    }
}
