namespace Cod
{
    public static class IEntityExtensions
    {
        public static StorageKey GetKey(this IEntity entity)
        {
            return new StorageKey { PartitionKey = entity.PartitionKey, RowKey = entity.RowKey };
        }
    }
}
