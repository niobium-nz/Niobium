namespace Cod.Platform.Database
{
    public interface IRepository<T>
    {
        Task<T> RetrieveAsync(string partitionKey, string rowKey, IList<string> fields = null, CancellationToken cancellationToken = default);

        Task<IEnumerable<T>> CreateAsync(IEnumerable<T> entities, bool replaceIfExist = false, DateTimeOffset? expiry = null, CancellationToken cancellationToken = default);

        Task<IEnumerable<T>> UpdateAsync(IEnumerable<T> entities, bool preconditionCheck = true, CancellationToken cancellationToken = default);

        Task<IEnumerable<T>> DeleteAsync(IEnumerable<T> entities, bool preconditionCheck = true, bool successIfNotExist = false, CancellationToken cancellationToken = default);

        Task<TableQueryResult<T>> GetAsync(int limit, string continuationToken = null, IList<string> fields = null, CancellationToken cancellationToken = default);

        Task<TableQueryResult<T>> GetAsync(string partitionKey, int limit, string continuationToken = null, IList<string> fields = null, CancellationToken cancellationToken = default);

        IAsyncEnumerable<T> GetAsync(IList<string> fields = null, CancellationToken cancellationToken = default);

        IAsyncEnumerable<T> GetAsync(string partitionKey, IList<string> fields = null, CancellationToken cancellationToken = default);
    }
}
