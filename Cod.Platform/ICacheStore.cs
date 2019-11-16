using System;
using System.Threading.Tasks;

namespace Cod.Platform
{
    public interface ICacheStore
    {
        Task<T> GetAsync<T>(string partitionKey, string rowKey) where T : IConvertible;

        Task DeleteAsync(string partitionKey, string rowKey);

        Task SetAsync<T>(string partitionKey, string rowKey, T value, DateTimeOffset? expiry = null) where T : IConvertible;
    }
}
