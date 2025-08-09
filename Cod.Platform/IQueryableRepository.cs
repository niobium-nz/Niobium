namespace Cod.Platform
{
    public interface IQueryableRepository<T> : IRepository<T>
    {
        Task<EntityBag<T>> QueryAsync(string partitionKey, string rowKeyStart, string rowKeyEnd, int limit, IList<string>? fields = null, string? continuationToken = null, CancellationToken cancellationToken = default);

        Task<EntityBag<T>> QueryAsync(string partitionKeyStart, string partitionKeyEnd, int limit, IList<string>? fields = null, string? continuationToken = null, CancellationToken cancellationToken = default);

        Task<EntityBag<T>> QueryAsync(string partitionKeyStart, string partitionKeyEnd, string rowKeyStart, string rowKeyEnd, int limit, IList<string>? fields = null, string? continuationToken = null, CancellationToken cancellationToken = default);

        IAsyncEnumerable<T> QueryAsync(string partitionKey, string rowKeyStart, string rowKeyEnd, IList<string>? fields = null, CancellationToken cancellationToken = default);

        IAsyncEnumerable<T> QueryAsync(string partitionKeyStart, string partitionKeyEnd, IList<string>? fields = null, CancellationToken cancellationToken = default);

        IAsyncEnumerable<T> QueryAsync(string partitionKeyStart, string partitionKeyEnd, string rowKeyStart, string rowKeyEnd, IList<string>? fields = null, CancellationToken cancellationToken = default);
    }
}
