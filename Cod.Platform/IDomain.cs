namespace Cod.Platform
{
    public interface IDomain
    {
        bool Initialized { get; }

        void Initialize(string partitionKey, string rowkey);
    }

    public interface IDomain<T> : IDomain
    {
        void Initialize(T model);
    }
}
