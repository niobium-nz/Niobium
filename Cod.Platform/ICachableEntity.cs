namespace Cod.Platform
{
    public interface ICachableEntity : IEntity
    {
        DateTimeOffset GetExpiry(DateTimeOffset timeStart);

        IConvertible GetCache();

        void Initialize(string partitionKey, string rowKey, IConvertible value);
    }
}
