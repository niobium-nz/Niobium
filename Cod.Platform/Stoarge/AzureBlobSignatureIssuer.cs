using Azure.Storage.Blobs;
using Azure.Storage.Sas;

namespace Cod.Platform.Stoarge
{
    internal class AzureBlobSignatureIssuer : IBlobSignatureIssuer
    {
        private readonly BlobServiceClient client;

        public AzureBlobSignatureIssuer(BlobServiceClient client)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public Task<Uri> IssueAsync(string containerName, DateTimeOffset expires, BlobPermissions permissions, CancellationToken cancellationToken = default)
        {
            BlobContainerClient container = client.GetBlobContainerClient(containerName);
            if (!container.CanGenerateSasUri)
            {
                throw new NotSupportedException($"Connection string to Blob Endpoint: {client.AccountName} doesn't have a key parameter.");
            }

            BlobContainerSasPermissions internalPermissions = 0;
            if (permissions.HasFlag(BlobPermissions.Read))
            {
                internalPermissions |= BlobContainerSasPermissions.Read;
            }

            if (permissions.HasFlag(BlobPermissions.Add))
            {
                internalPermissions |= BlobContainerSasPermissions.Add;
            }

            if (permissions.HasFlag(BlobPermissions.Write))
            {
                internalPermissions |= BlobContainerSasPermissions.Write;
                internalPermissions |= BlobContainerSasPermissions.Tag;
            }

            if (permissions.HasFlag(BlobPermissions.Delete))
            {
                internalPermissions |= BlobContainerSasPermissions.Delete;
                internalPermissions |= BlobContainerSasPermissions.DeleteBlobVersion;
            }

            if (permissions.HasFlag(BlobPermissions.List))
            {
                internalPermissions |= BlobContainerSasPermissions.List;
            }

            if (permissions.HasFlag(BlobPermissions.Create))
            {
                internalPermissions |= BlobContainerSasPermissions.Create;
            }

            return internalPermissions == 0
                ? throw new UnauthorizedAccessException("Cannot create SAS without any permission granted.")
                : Task.FromResult(container.GenerateSasUri(internalPermissions, expires));
        }
    }
}
