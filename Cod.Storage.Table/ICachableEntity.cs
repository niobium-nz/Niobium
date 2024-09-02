namespace Cod.Storage.Table
{
    public interface ICachableEntity
    {
        DateTimeOffset GetExpiry(DateTimeOffset timeStart);

        IConvertible GetCache();

        void Initialize(string partitionKey, string rowKey, IConvertible value);
    }
}
