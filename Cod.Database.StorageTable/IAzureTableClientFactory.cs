using Azure.Data.Tables;

namespace Cod.Database.StorageTable
{
    public interface IAzureTableClientFactory
    {
        Task<TableServiceClient> CreateClientAsync(IEnumerable<DatabasePermissions> permissions, string table, string? partition = null, CancellationToken cancellationToken = default);
    }
}
