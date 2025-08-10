using Azure.Storage;
using Azure.Storage.Sas;
using Cod.File;
using Cod.File.Blob;
using Microsoft.Extensions.Options;

namespace Cod.Platform.Blob
{
    internal sealed class AzureBlobSignatureIssuer(IOptions<StorageBlobOptions> options) : ISignatureIssuer
    {
        public bool CanIssue(ResourceType storageType, StorageControl control)
        {
            return storageType == ResourceType.AzureStorageBlob
                && !string.IsNullOrWhiteSpace(options.Value.Key)
                && !string.IsNullOrWhiteSpace(options.Value.FullyQualifiedDomainName);
        }

        public Task<(string, DateTimeOffset)> IssueAsync(ResourceType storageType, StorageControl control, DateTimeOffset expires, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(options.Value.Key))
            {
                throw new ApplicationException(InternalError.InternalServerError);
            }

            if (control.Resource != options.Value.FullyQualifiedDomainName)
            {
                throw new ApplicationException(InternalError.InternalServerError);
            }

            if (control.StartPartitionKey != control.EndPartitionKey)
            {
                throw new ApplicationException(InternalError.InternalServerError);
            }

            FilePermissions permissions = (FilePermissions)control.Permission;
            BlobContainerSasPermissions internalPermissions = 0;
            if (permissions.HasFlag(FilePermissions.Read))
            {
                internalPermissions |= BlobContainerSasPermissions.Read;
            }

            if (permissions.HasFlag(FilePermissions.Add))
            {
                internalPermissions |= BlobContainerSasPermissions.Add;
            }

            if (permissions.HasFlag(FilePermissions.Write))
            {
                internalPermissions |= BlobContainerSasPermissions.Write;
                internalPermissions |= BlobContainerSasPermissions.Tag;
            }

            if (permissions.HasFlag(FilePermissions.Delete))
            {
                internalPermissions |= BlobContainerSasPermissions.Delete;
                internalPermissions |= BlobContainerSasPermissions.DeleteBlobVersion;
            }

            if (permissions.HasFlag(FilePermissions.List))
            {
                internalPermissions |= BlobContainerSasPermissions.List;
            }

            if (permissions.HasFlag(FilePermissions.Create))
            {
                internalPermissions |= BlobContainerSasPermissions.Create;
            }

            if (internalPermissions == 0)
            {
                throw new ApplicationException(InternalError.Forbidden);
            }

            BlobSasBuilder builder = new(internalPermissions, expires)
            {
                BlobContainerName = control.StartPartitionKey
            };
            string accountName = ParseAccountName(options.Value.FullyQualifiedDomainName);
            StorageSharedKeyCredential cred = new(accountName, options.Value.Key);
            BlobSasQueryParameters sas = builder.ToSasQueryParameters(cred);

            return Task.FromResult((sas.ToString(), expires));
        }

        private static string ParseAccountName(string fqdn)
        {
            return fqdn.Split('.').First();
        }
    }
}
