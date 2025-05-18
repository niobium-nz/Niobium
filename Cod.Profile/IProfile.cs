namespace Cod.Profile
{
    public interface IProfile
    {
        string PartitionKey { get; set; }

        Guid RowKey { get; set; }

        DateTimeOffset? Timestamp { get; set; }

        DateTimeOffset? CreatedAt { get; set; }

        string? ETag { get; set; }
    }
}
