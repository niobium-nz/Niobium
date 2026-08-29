using Azure.Storage;
using Azure.Storage.Sas;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Niobium.File;
using Niobium.File.Blob;

namespace Niobium.Platform.Blob
{
    internal sealed class AzureBlobSignatureIssuer(IOptions<StorageBlobOptions> options, IConfiguration configuration) : ISignatureIssuer
    {
        public bool CanIssue(ResourceType storageType, StorageControl control)
            => storageType == ResourceType.AzureStorageBlob
                && !String.IsNullOrWhiteSpace(options.Value.Key)
                && (!String.IsNullOrWhiteSpace(options.Value.FullyQualifiedDomainName)
                    || !String.IsNullOrWhiteSpace(configuration[File.Blob.Constants.DefaultBlobServiceUriSetting]));

        public Task<(string, DateTimeOffset)> IssueAsync(ResourceType storageType, StorageControl control, DateTimeOffset expires, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(options.Value.Key))
            {
                throw new ApplicationException(Niobium.InternalError.InternalServerError);
            }

            if (control.Resource != options.Value.FullyQualifiedDomainName)
            {
                throw new ApplicationException(Niobium.InternalError.InternalServerError);
            }

            if (control.StartPartitionKey != control.EndPartitionKey)
            {
                throw new ApplicationException(Niobium.InternalError.InternalServerError);
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
                throw new ApplicationException(Niobium.InternalError.Forbidden);
            }

            BlobSasBuilder builder = new(internalPermissions, expires)
            {
                BlobContainerName = control.StartPartitionKey
            };

            string fdqn = options.Value.FullyQualifiedDomainName ?? configuration[File.Blob.Constants.DefaultBlobServiceUriSetting]!;
            if (Uri.TryCreate(fdqn, UriKind.Absolute, out Uri? fdqnUri))
            {
                fdqn = fdqnUri.Host;
            }

            string accountName = ParseAccountName(fdqn);
            StorageSharedKeyCredential cred = new(accountName, options.Value.Key);
            BlobSasQueryParameters sas = builder.ToSasQueryParameters(cred);

            return Task.FromResult((sas.ToString(), expires));
        }

        private static string ParseAccountName(string fqdn) => fqdn.Split('.').First();
    }
}
