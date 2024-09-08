using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Cod.Platform;

namespace Cod.Storage.Blob
{
    internal class AzureBlobSignatureIssuer : ISignatureIssuer
    {
        private readonly BlobServiceClient client;

        public AzureBlobSignatureIssuer(BlobServiceClient client)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public bool CanIssue(ResourceType storageType, StorageControl control)
        {
            return storageType == ResourceType.AzureStorageBlob;
        }

        public Task<Uri> IssueAsync(ResourceType storageType, StorageControl control, DateTimeOffset expires, CancellationToken cancellationToken = default)
        {
            BlobContainerClient container = client.GetBlobContainerClient(control.Resource);
            if (!container.CanGenerateSasUri)
            {
                throw new NotSupportedException($"Connection string to Blob Endpoint: {client.AccountName} doesn't have a key parameter.");
            }

            BlobPermissions permissions = (BlobPermissions)control.Permission;
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
