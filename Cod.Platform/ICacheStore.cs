namespace Cod.Platform
{
    public interface ICacheStore
    {
        Task<T?> GetAsync<T>(string partitionKey, string rowKey, CancellationToken cancellationToken = default) where T : IConvertible;

        Task DeleteAsync(string partitionKey, string rowKey, CancellationToken cancellationToken = default);

        Task SetAsync<T>(string partitionKey, string rowKey, T value, bool memoryCached, DateTimeOffset? expiry = null, CancellationToken cancellationToken = default) where T : IConvertible;
    }
}
