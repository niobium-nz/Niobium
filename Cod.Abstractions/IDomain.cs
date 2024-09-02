namespace Cod
{
    public interface IDomain
    {
        string PartitionKey { get; }

        string RowKey { get; }

        bool Initialized { get; }

        Task<string> GetHashAsync();
    }

    public interface IDomain<T> : IDomain
    {
        Task<T> GetEntityAsync();

        IDomain<T> Initialize(T entity);

        IDomain<T> Initialize(string partitionKey, string rowKey);
    }
}
