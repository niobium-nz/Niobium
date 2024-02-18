namespace Cod.Platform.Stoarge
{
    public interface IBlobSignatureIssuer
    {
        Task<Uri> IssueAsync(string containerName, DateTimeOffset expires, BlobPermissions permissions, CancellationToken cancellationToken = default);
    }
}
