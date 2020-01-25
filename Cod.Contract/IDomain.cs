namespace Cod
{
    public interface IDomain
    {
        string PartitionKey { get; }

        string RowKey { get; }

        bool Initialized { get; }
    }

    public interface IDomain<T> : IDomain
    {
        IDomain<T> Initialize(T entity);
    }
}
