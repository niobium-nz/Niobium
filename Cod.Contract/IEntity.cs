namespace Cod
{
    public interface IEntity
    {
        string PartitionKey { get; }

        string RowKey { get; }
    }
}
