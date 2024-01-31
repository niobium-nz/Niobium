namespace Cod.Platform
{
    public interface IBlobSignatureIssuer
    {
        Task<Uri> IssueAsync(string containerName, DateTimeOffset expires, BlobPermissions permissions, CancellationToken cancellationToken = default);
    }
}
