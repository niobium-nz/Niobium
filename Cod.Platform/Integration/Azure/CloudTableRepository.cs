using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;

namespace Cod.Platform
{
    public class CloudTableRepository<T> : IRepository<T>, IQueryableRepository<T> where T : ITableEntity, IEntity, new()
    {
        private readonly string tableName;

        public CloudTableRepository()
        {
        }

        public CloudTableRepository(string tableName)
        {
            this.tableName = tableName;
        }

        public async Task<IEnumerable<T>> CreateAsync(IEnumerable<T> entities, bool replaceIfExist)
        {
            foreach (var entity in entities)
            {
                if (!entity.Created.HasValue)
                {
                    entity.Created = DateTimeOffset.UtcNow;
                }
            }

            if (replaceIfExist)
            {
                return await this.GetTable().InsertOrReplaceAsync(entities);
            }
            else
            {
                return await this.GetTable().InsertAsync(entities);
            }
        }

        public async Task<IEnumerable<T>> UpdateAsync(IEnumerable<T> entities)
            => await this.GetTable().ReplaceAsync(entities);

        public async Task<IEnumerable<T>> CreateOrUpdateAsync(IEnumerable<T> entities)
            => await this.GetTable().InsertOrReplaceAsync(entities);

        public async Task<IEnumerable<T>> DeleteAsync(IEnumerable<T> entities, bool successIfNotExist = false)
            => await this.GetTable().RemoveAsync(entities, successIfNotExist: successIfNotExist);

        public async Task<TableQueryResult<T>> GetAsync(string partitionKey, int limit)
            => await this.GetTable().WhereAsync<T>(
                TableQuery.GenerateFilterCondition(nameof(ITableEntity.PartitionKey), QueryComparisons.Equal, partitionKey),
                takeCount: limit);

        public async Task<T> GetAsync(string partitionKey, string rowKey)
            => await this.GetTable().RetrieveAsync<T>(partitionKey, rowKey);

        public async Task<TableQueryResult<T>> GetAsync(int limit)
            => await this.GetTable().WhereAsync<T>(takeCount: limit);

        public async Task<TableQueryResult<T>> GetAsync(string partitionKey, string rowKeyStart, string rowKeyEnd, int limit = -1)
            => await this.GetTable().WhereAsync<T>(
                TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition(nameof(ITableEntity.PartitionKey), QueryComparisons.Equal, partitionKey),
                TableOperators.And,
                    TableQuery.CombineFilters(
                        TableQuery.GenerateFilterCondition(nameof(ITableEntity.RowKey), QueryComparisons.GreaterThanOrEqual, rowKeyStart),
                    TableOperators.And,
                        TableQuery.GenerateFilterCondition(nameof(ITableEntity.RowKey), QueryComparisons.LessThanOrEqual, rowKeyEnd))),
                takeCount: limit);

        public async Task<TableQueryResult<T>> GetAsync(string partitionKey, string rowKeyStart, string rowKeyEnd, IList<string> fields, int limit = -1)
            => await this.GetTable().WhereAsync<T>(
                TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition(nameof(ITableEntity.PartitionKey), QueryComparisons.Equal, partitionKey),
                TableOperators.And,
                    TableQuery.CombineFilters(
                        TableQuery.GenerateFilterCondition(nameof(ITableEntity.RowKey), QueryComparisons.GreaterThanOrEqual, rowKeyStart),
                    TableOperators.And,
                        TableQuery.GenerateFilterCondition(nameof(ITableEntity.RowKey), QueryComparisons.LessThanOrEqual, rowKeyEnd))),
                fields: fields,
                takeCount: limit);

        public async Task<TableQueryResult<T>> GetAsync(string partitionKey, IList<string> fields, int limit = -1)
            => await this.GetTable().WhereAsync<T>(
                TableQuery.GenerateFilterCondition(nameof(ITableEntity.PartitionKey), QueryComparisons.Equal, partitionKey),
                takeCount: limit, fields: fields);

        public async Task<TableQueryResult<T>> GetAsync(string partitionKeyStart, string partitionKeyEnd, IList<string> fields, int limit = -1)
          => await this.GetTable().WhereAsync<T>(
                    TableQuery.CombineFilters(
                        TableQuery.GenerateFilterCondition(nameof(ITableEntity.PartitionKey), QueryComparisons.GreaterThanOrEqual, partitionKeyStart),
                    TableOperators.And,
                        TableQuery.GenerateFilterCondition(nameof(ITableEntity.PartitionKey), QueryComparisons.LessThanOrEqual, partitionKeyEnd)),
                fields: fields,
                takeCount: limit);

        public async Task<TableQueryResult<T>> GetAsync(string partitionKeyStart, string partitionKeyEnd, string rowKeyStart, string rowKeyEnd, int limit = -1)
            => await this.GetAsync(partitionKeyStart, partitionKeyEnd, rowKeyStart, rowKeyEnd, null, limit: limit);

        public async Task<TableQueryResult<T>> GetAsync(string partitionKeyStart, string partitionKeyEnd, string rowKeyStart, string rowKeyEnd, IList<string> fields, int limit = -1)
          => await this.GetTable().WhereAsync<T>(
                TableQuery.CombineFilters(
                    TableQuery.CombineFilters(
                        TableQuery.GenerateFilterCondition(nameof(ITableEntity.PartitionKey), QueryComparisons.GreaterThanOrEqual, partitionKeyStart),
                    TableOperators.And,
                        TableQuery.GenerateFilterCondition(nameof(ITableEntity.PartitionKey), QueryComparisons.LessThanOrEqual, partitionKeyEnd)),
                TableOperators.And,
                    TableQuery.CombineFilters(
                        TableQuery.GenerateFilterCondition(nameof(ITableEntity.RowKey), QueryComparisons.GreaterThanOrEqual, rowKeyStart),
                    TableOperators.And,
                        TableQuery.GenerateFilterCondition(nameof(ITableEntity.RowKey), QueryComparisons.LessThanOrEqual, rowKeyEnd))),
                fields: fields,
                takeCount: limit);

        private CloudTable GetTable()
        {
            if (string.IsNullOrEmpty(this.tableName))
            {
                return CloudStorage.GetTable<T>();
            }
            else
            {
                return CloudStorage.GetTable(this.tableName);
            }
        }
    }
}
