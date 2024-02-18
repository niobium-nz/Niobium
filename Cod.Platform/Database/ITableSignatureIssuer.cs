namespace Cod.Platform.Database
{
    public interface ITableSignatureIssuer
    {
        Task<Uri> IssueAsync(DateTimeOffset expires, StorageControl control, CancellationToken cancellationToken = default);
    }
}
