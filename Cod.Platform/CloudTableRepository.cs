using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;

namespace Cod.Platform
{
    public class CloudTableRepository<T> : IRepository<T> where T : ITableEntity, new()
    {
        public async Task<IEnumerable<T>> CreateAsync(IEnumerable<T> entities, bool replaceIfExist, ILogger logger)
        {
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

        public async Task<IEnumerable<T>> DeleteAsync(IEnumerable<T> entities)
            => await CloudStorage.GetTable<T>().RemoveAsync(entities);

        public async Task<TableQueryResult<T>> GetAsync(string partitionKey, int limit)
            => await CloudStorage.GetTable<T>().WhereAsync<T>(
                TableQuery.GenerateFilterCondition(nameof(ITableEntity.PartitionKey), QueryComparisons.Equal, partitionKey),
                takeCount: limit);

        public async Task<T> GetAsync(string partitionKey, string rowKey)
            => await CloudStorage.GetTable<T>().RetrieveAsync<T>(partitionKey, rowKey);

        public async Task<TableQueryResult<T>> GetAsync(int limit)
            => await CloudStorage.GetTable<T>().WhereAsync<T>(takeCount: limit);
    }
}
