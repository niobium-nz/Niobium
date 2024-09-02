using Azure.Data.Tables;
using Azure.Data.Tables.Sas;
using Cod.Platform;

namespace Cod.Storage.Table
{
    internal class AzureTableSignatureIssuer : ISignatureIssuer
    {
        private readonly TableServiceClient client;

        public AzureTableSignatureIssuer(TableServiceClient client)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public bool CanIssue(StorageType storageType, StorageControl control)
        {
            return storageType == StorageType.Table;
        }

        public Task<Uri> IssueAsync(StorageType storageType, StorageControl control, DateTimeOffset expires, CancellationToken cancellationToken = default)
        {
            TableClient table = client.GetTableClient(control.Resource);
            TablePermissions permissions = (TablePermissions)control.Permission;
            TableSasPermissions internalPermissions = 0;
            if (permissions.HasFlag(TablePermissions.Query))
            {
                internalPermissions |= TableSasPermissions.Read;
            }

            if (permissions.HasFlag(TablePermissions.Add))
            {
                internalPermissions |= TableSasPermissions.Add;
            }

            if (permissions.HasFlag(TablePermissions.Update))
            {
                internalPermissions |= TableSasPermissions.Update;
            }

            if (permissions.HasFlag(TablePermissions.Delete))
            {
                internalPermissions |= TableSasPermissions.Delete;
            }

            TableSasBuilder builder = table.GetSasBuilder(internalPermissions, expires);
            builder.PartitionKeyStart = control.StartPartitionKey;
            builder.PartitionKeyEnd = control.EndPartitionKey;
            builder.RowKeyStart = control.StartRowKey;
            builder.RowKeyEnd = control.EndRowKey;

            return internalPermissions == 0
                ? throw new UnauthorizedAccessException("Cannot create SAS without any permission granted.")
                : Task.FromResult(table.GenerateSasUri(builder));
        }
    }
}
