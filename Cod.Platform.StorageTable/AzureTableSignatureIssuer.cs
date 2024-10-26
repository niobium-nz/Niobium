using Azure.Data.Tables;
using Azure.Data.Tables.Sas;
using Cod.Database.StorageTable;
using Microsoft.Extensions.Options;

namespace Cod.Platform.StorageTable
{
    internal class AzureTableSignatureIssuer(IOptions<StorageTableOptions> options) : ISignatureIssuer
    {
        public bool CanIssue(ResourceType storageType, StorageControl control)
        {
            return storageType == ResourceType.AzureStorageTable
                && !string.IsNullOrWhiteSpace(options.Value.Key)
                && !string.IsNullOrWhiteSpace(options.Value.FullyQualifiedDomainName)
                && !string.IsNullOrWhiteSpace(control.Resource);
        }

        public Task<(string, DateTimeOffset)> IssueAsync(ResourceType storageType, StorageControl control, DateTimeOffset expires, CancellationToken cancellationToken = default)
        {
            DatabasePermissions permissions = (DatabasePermissions)control.Permission;
            TableSasPermissions internalPermissions = 0;
            if (permissions.HasFlag(DatabasePermissions.Query))
            {
                internalPermissions |= TableSasPermissions.Read;
            }

            if (permissions.HasFlag(DatabasePermissions.Add))
            {
                internalPermissions |= TableSasPermissions.Add;
            }

            if (permissions.HasFlag(DatabasePermissions.Update))
            {
                internalPermissions |= TableSasPermissions.Update;
            }

            if (permissions.HasFlag(DatabasePermissions.Delete))
            {
                internalPermissions |= TableSasPermissions.Delete;
            }

            if (internalPermissions == 0)
            {
                throw new ApplicationException(InternalError.Forbidden);
            }

            TableSasBuilder builder = new(control.Resource, internalPermissions, expires)
            {
                PartitionKeyStart = control.StartPartitionKey,
                PartitionKeyEnd = control.EndPartitionKey,
                RowKeyStart = control.StartRowKey,
                RowKeyEnd = control.EndRowKey,
            };
            var accountName = ParseAccountName(options.Value.FullyQualifiedDomainName);
            var cred = new TableSharedKeyCredential(accountName, options.Value.Key);
            var sas = builder.ToSasQueryParameters(cred);

            return Task.FromResult((sas.ToString(), expires));
        }

        private static string ParseAccountName(string fqdn) => fqdn.Split('.').First();
    }
}
