using Azure.Data.Tables;
using Azure.Data.Tables.Sas;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Niobium.Database.StorageTable;

namespace Niobium.Platform.StorageTable
{
    internal sealed class AzureTableSignatureIssuer(IOptions<StorageTableOptions> options, IConfiguration configuration) : ISignatureIssuer
    {
        public bool CanIssue(ResourceType storageType, StorageControl control) => storageType == ResourceType.AzureStorageTable
                && !String.IsNullOrWhiteSpace(options.Value.Key)
                && (!String.IsNullOrWhiteSpace(options.Value.FullyQualifiedDomainName) || !String.IsNullOrWhiteSpace(configuration[Database.StorageTable.Constants.DefaultTableServiceUriSetting]))
                && !String.IsNullOrWhiteSpace(control.Resource);

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
                throw new ApplicationException(Niobium.InternalError.Forbidden);
            }

            TableSasBuilder builder = new(control.Resource, internalPermissions, expires)
            {
                PartitionKeyStart = control.StartPartitionKey,
                PartitionKeyEnd = control.EndPartitionKey,
                RowKeyStart = control.StartRowKey,
                RowKeyEnd = control.EndRowKey,
            };

            string fdqn = options.Value.FullyQualifiedDomainName ?? configuration[Database.StorageTable.Constants.DefaultTableServiceUriSetting]!;
            if (Uri.TryCreate(fdqn, UriKind.Absolute, out Uri? fdqnUri))
            {
                fdqn = fdqnUri.Host;
            }

            string accountName = ParseAccountName(fdqn);
            TableSharedKeyCredential cred = new(accountName, options.Value.Key);
            TableSasQueryParameters sas = builder.ToSasQueryParameters(cred);

            return Task.FromResult((sas.ToString(), expires));
        }

        private static string ParseAccountName(string fqdn) => fqdn.Split('.').First();
    }
}
