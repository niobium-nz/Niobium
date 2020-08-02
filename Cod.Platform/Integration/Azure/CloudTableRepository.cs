using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;

namespace Cod.Platform
{
    public class CloudTableRepository<T> : IRepository<T>, IQueryableRepository<T> where T : ITableEntity, IEntity, new()
    {
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
                return await CloudStorage.GetTable<T>().InsertOrReplaceAsync(entities);
            }
            else
            {
                return await CloudStorage.GetTable<T>().InsertAsync(entities);
            }
        }

        public async Task<IEnumerable<T>> UpdateAsync(IEnumerable<T> entities)
            => await CloudStorage.GetTable<T>().ReplaceAsync(entities);

        public async Task<IEnumerable<T>> CreateOrUpdateAsync(IEnumerable<T> entities)
            => await CloudStorage.GetTable<T>().InsertOrReplaceAsync(entities);

        public async Task<IEnumerable<T>> DeleteAsync(IEnumerable<T> entities, bool successIfNotExist = false)
            => await CloudStorage.GetTable<T>().RemoveAsync(entities, successIfNotExist);

        public async Task<TableQueryResult<T>> GetAsync(string partitionKey, int limit)
            => await CloudStorage.GetTable<T>().WhereAsync<T>(
                TableQuery.GenerateFilterCondition(nameof(ITableEntity.PartitionKey), QueryComparisons.Equal, partitionKey),
                takeCount: limit);

        public async Task<T> GetAsync(string partitionKey, string rowKey)
            => await CloudStorage.GetTable<T>().RetrieveAsync<T>(partitionKey, rowKey);

        public async Task<TableQueryResult<T>> GetAsync(int limit)
            => await CloudStorage.GetTable<T>().WhereAsync<T>(takeCount: limit);

        public async Task<TableQueryResult<T>> GetAsync(string partitionKey, string rowKeyStart, string rowKeyEnd, int limit = -1)
            => await CloudStorage.GetTable<T>().WhereAsync<T>(
                TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition(nameof(ITableEntity.PartitionKey), QueryComparisons.Equal, partitionKey),
                TableOperators.And,
                    TableQuery.CombineFilters(
                        TableQuery.GenerateFilterCondition(nameof(ITableEntity.RowKey), QueryComparisons.GreaterThanOrEqual, rowKeyStart),
                    TableOperators.And,
                        TableQuery.GenerateFilterCondition(nameof(ITableEntity.RowKey), QueryComparisons.LessThanOrEqual, rowKeyEnd))),
                takeCount: limit);
    }
}
