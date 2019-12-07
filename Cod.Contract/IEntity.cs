namespace Cod.Contract
{
    public interface IEntity
    {
        string PartitionKey { get; set; }

        string RowKey { get; set; }
    }
}
