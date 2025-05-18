namespace Cod.Profile
{
    public interface IProfile
    {
        Guid PartitionKey { get; set; }

        Guid RowKey { get; set; }

        DateTimeOffset? Timestamp { get; set; }

        string? ETag { get; set; }
    }
}
