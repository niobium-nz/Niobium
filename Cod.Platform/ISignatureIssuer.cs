namespace Cod.Platform
{
    public interface ISignatureIssuer
    {
        bool CanIssue(StorageType storageType, StorageControl control);

        Task<Uri> IssueAsync(StorageType storageType, StorageControl control, DateTimeOffset expires, CancellationToken cancellationToken = default);
    }
}
