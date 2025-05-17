namespace Cod
{
    public interface IRepository<T>
    {
        Task<T> RetrieveAsync(string partitionKey, string rowKey, IList<string> fields = null, CancellationToken cancellationToken = default);

        Task<IEnumerable<T>> CreateAsync(IEnumerable<T> entities, bool replaceIfExist = false, DateTimeOffset? expiry = null, CancellationToken cancellationToken = default);

        Task<IEnumerable<T>> UpdateAsync(IEnumerable<T> entities, bool preconditionCheck = true, bool mergeIfExists = false, CancellationToken cancellationToken = default);

        Task DeleteAsync(IEnumerable<T> entities, bool preconditionCheck = true, bool successIfNotExist = true, CancellationToken cancellationToken = default);

        Task<EntityBag<T>> GetAsync(int limit, string continuationToken = null, IList<string> fields = null, CancellationToken cancellationToken = default);

        Task<EntityBag<T>> GetAsync(string partitionKey, int limit, string continuationToken = null, IList<string> fields = null, CancellationToken cancellationToken = default);

        IAsyncEnumerable<T> GetAsync(IList<string> fields = null, CancellationToken cancellationToken = default);

        IAsyncEnumerable<T> GetAsync(string partitionKey, IList<string> fields = null, CancellationToken cancellationToken = default);
    }
}
