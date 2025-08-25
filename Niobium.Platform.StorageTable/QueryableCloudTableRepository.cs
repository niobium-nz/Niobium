using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using Niobium.Database.StorageTable;

namespace Niobium.Platform.StorageTable
{
    public class QueryableCloudTableRepository<T>(IAzureTableClientFactory clientFactory, ILogger<CloudTableRepository<T>> logger)
        : CloudTableRepository<T>(clientFactory, logger), IQueryableRepository<T> where T : class, new()
    {
        private const string And = " and ";
        private const string GreaterThanOrEqual = "ge";
        private const string LessThanOrEqual = "le";

        public async Task<EntityBag<T>> QueryAsync(string partitionKey, string rowKeyStart, string rowKeyEnd, int limit, IList<string>? fields = null, string? continuationToken = null, CancellationToken cancellationToken = default)
        {
            return await QueryAsync(filter: string.Join(And,
            [
                $"{nameof(ITableEntity.PartitionKey)} {Equal} '{partitionKey}'",
                $"{nameof(ITableEntity.RowKey)} {GreaterThanOrEqual} '{rowKeyStart}'",
                $"{nameof(ITableEntity.RowKey)} {LessThanOrEqual} '{rowKeyEnd}'",
            ]), fields: fields, limit: limit, continuationToken: continuationToken, cancellationToken: cancellationToken);
        }

        public async Task<EntityBag<T>> QueryAsync(string partitionKeyStart, string partitionKeyEnd, int limit, IList<string>? fields = null, string? continuationToken = null, CancellationToken cancellationToken = default)
        {
            return await QueryAsync(filter: string.Join(And,
            [
                $"{nameof(ITableEntity.PartitionKey)} {GreaterThanOrEqual} '{partitionKeyStart}'",
                $"{nameof(ITableEntity.PartitionKey)} {LessThanOrEqual} '{partitionKeyEnd}'",
            ]), fields: fields, limit: limit, continuationToken: continuationToken, cancellationToken: cancellationToken);
        }

        public async Task<EntityBag<T>> QueryAsync(string partitionKeyStart, string partitionKeyEnd, string rowKeyStart, string rowKeyEnd, int limit, IList<string>? fields = null, string? continuationToken = null, CancellationToken cancellationToken = default)
        {
            return await QueryAsync(filter: string.Join(And,
            [
                $"{nameof(ITableEntity.PartitionKey)} {GreaterThanOrEqual} '{partitionKeyStart}'",
                $"{nameof(ITableEntity.PartitionKey)} {LessThanOrEqual} '{partitionKeyEnd}'",
                $"{nameof(ITableEntity.RowKey)} {GreaterThanOrEqual} '{rowKeyStart}'",
                $"{nameof(ITableEntity.RowKey)} {LessThanOrEqual} '{rowKeyEnd}'",
            ]), fields: fields, limit: limit, continuationToken: continuationToken, cancellationToken: cancellationToken);
        }

        public IAsyncEnumerable<T> QueryAsync(string partitionKey, string rowKeyStart, string rowKeyEnd, IList<string>? fields = null, CancellationToken cancellationToken = default)
        {
            return QueryAsync(filter: string.Join(And,
            [
                $"{nameof(ITableEntity.PartitionKey)} {Equal} '{partitionKey}'",
                $"{nameof(ITableEntity.RowKey)} {GreaterThanOrEqual} '{rowKeyStart}'",
                $"{nameof(ITableEntity.RowKey)} {LessThanOrEqual} '{rowKeyEnd}'",
            ]), fields: fields, cancellationToken: cancellationToken);
        }

        public IAsyncEnumerable<T> QueryAsync(string partitionKeyStart, string partitionKeyEnd, IList<string>? fields = null, CancellationToken cancellationToken = default)
        {
            return QueryAsync(filter: string.Join(And,
            [
                $"{nameof(ITableEntity.PartitionKey)} {GreaterThanOrEqual} '{partitionKeyStart}'",
                $"{nameof(ITableEntity.PartitionKey)} {LessThanOrEqual} '{partitionKeyEnd}'",
            ]), fields: fields, cancellationToken: cancellationToken);
        }

        public IAsyncEnumerable<T> QueryAsync(string partitionKeyStart, string partitionKeyEnd, string rowKeyStart, string rowKeyEnd, IList<string>? fields = null, CancellationToken cancellationToken = default)
        {
            return QueryAsync(filter: string.Join(And,
            [
                $"{nameof(ITableEntity.PartitionKey)} {GreaterThanOrEqual} '{partitionKeyStart}'",
                $"{nameof(ITableEntity.PartitionKey)} {LessThanOrEqual} '{partitionKeyEnd}'",
                $"{nameof(ITableEntity.RowKey)} {GreaterThanOrEqual} '{rowKeyStart}'",
                $"{nameof(ITableEntity.RowKey)} {LessThanOrEqual} '{rowKeyEnd}'",
            ]), fields: fields, cancellationToken: cancellationToken);
        }
    }
}
