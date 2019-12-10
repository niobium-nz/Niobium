namespace Cod.Contract
{
    public interface IEntity
    {
        string PartitionKey { get; }

        string RowKey { get; }
    }
}
