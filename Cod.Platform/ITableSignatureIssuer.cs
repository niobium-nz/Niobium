namespace Cod.Platform
{
    public interface ITableSignatureIssuer
    {
        Task<Uri> IssueAsync(DateTimeOffset expires, StorageControl control, CancellationToken cancellationToken = default);
    }
}
