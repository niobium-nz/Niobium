namespace Cod.Platform
{
    public interface ISignatureIssuer
    {
        bool CanIssue(ResourceType type, StorageControl control);

        Task<(string, DateTimeOffset)> IssueAsync(ResourceType type, StorageControl control, DateTimeOffset expires, CancellationToken cancellationToken = default);
    }
}
