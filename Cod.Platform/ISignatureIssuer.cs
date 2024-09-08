namespace Cod.Platform
{
    public interface ISignatureIssuer
    {
        bool CanIssue(ResourceType storageType, StorageControl control);

        Task<Uri> IssueAsync(ResourceType storageType, StorageControl control, DateTimeOffset expires, CancellationToken cancellationToken = default);
    }
}
