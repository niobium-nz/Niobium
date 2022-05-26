using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace Cod.Platform
{
    internal static class CloudTableExtensions
    {
        public static async Task<IEnumerable<T>> InsertAsync<T>(this CloudTable table, IEnumerable<T> entitiesToCreate) where T : ITableEntity, new()
        {
            await table.ExecuteBatchAsync(entitiesToCreate, (batch, entity) => batch.Insert(entity));
            return entitiesToCreate;
        }

        public static async Task<IEnumerable<T>> ReplaceAsync<T>(this CloudTable table, IEnumerable<T> entitiesToReplace) where T : ITableEntity, new()
        {
            await table.ExecuteBatchAsync(entitiesToReplace, (batch, entity) => batch.Replace(entity));
            return entitiesToReplace;
        }

        public static async Task<IList<T>> MergeAsync<T>(this CloudTable table,
            Func<Task<IEnumerable<T>>> getEntitiesToReplace,
            Func<T, Task<bool>> changeOperation,
            bool retryOnConfliction = false)
             where T : ITableEntity, new()
        {
            for (var i = 0; i < 3; i++)
            {
                var entitiesToReplace = new List<T>();
                var entitiesToChange = await getEntitiesToReplace();
                foreach (var entity in entitiesToChange)
                {
                    var changed = await changeOperation(entity);
                    if (changed)
                    {
                        entitiesToReplace.Add(entity);
                    }
                }
                try
                {
                    await table.ExecuteBatchAsync(entitiesToReplace, (batch, entity) => batch.Merge(entity));
                    return entitiesToReplace;
                }
                catch (StorageException e)
                {
                    if (e.RequestInformation.HttpStatusCode == 400 || e.RequestInformation.HttpStatusCode == 412)
                    {
                        if (i + 1 == 3)
                        {
                            throw;
                        }

                        if (retryOnConfliction)
                        {
                            await Task.Delay(100);
                            continue;
                        }
                    }
                    throw;
                }
            }
            return new List<T>();
        }

        public static async Task<IEnumerable<T>> InsertOrReplaceAsync<T>(this CloudTable table, IEnumerable<T> entitiesToReplace) where T : ITableEntity, new()
        {
            await table.ExecuteBatchAsync(entitiesToReplace, (batch, entity) => batch.InsertOrReplace(entity));
            return entitiesToReplace;
        }

        public static async Task<IEnumerable<T>> RemoveAsync<T>(
            this CloudTable table,
            IEnumerable<T> entitiesToRemove,
            bool retryOnConfliction = false,
            bool successIfNotExist = false)
            where T : ITableEntity, new()
        {
            for (var i = 0; i < 3; i++)
            {
                try
                {
                    await table.ExecuteBatchAsync(entitiesToRemove, (batch, entity) => batch.Delete(entity));
                    return entitiesToRemove;
                }
                catch (StorageException e)
                {
                    if (successIfNotExist && e.RequestInformation.HttpStatusCode == 404)
                    {
                        return Enumerable.Empty<T>();
                    }

                    if (e.RequestInformation.HttpStatusCode == 400 || e.RequestInformation.HttpStatusCode == 412)
                    {
                        if (i + 1 == 3)
                        {
                            throw;
                        }

                        if (retryOnConfliction)
                        {
                            await Task.Delay(100);
                            continue;
                        }
                    }
                    throw;
                }
            }

            throw new NotImplementedException();
        }

        public static async Task RemoveAsync<T>(this CloudTable table,
            Func<Task<IEnumerable<T>>> getEntitiesToRemove,
            Func<T, Task<bool>> predicate,
            bool retryOnConfliction = false,
            bool successIfNotExist = false)
             where T : ITableEntity, new()
        {
            for (var i = 0; i < 3; i++)
            {
                var entitiesToRemove = new List<T>();
                var entitiesToTest = await getEntitiesToRemove();
                foreach (var entity in entitiesToTest)
                {
                    var success = await predicate(entity);
                    if (success)
                    {
                        entitiesToRemove.Add(entity);
                    }
                }
                try
                {
                    await table.ExecuteBatchAsync(entitiesToRemove, (batch, entity) => batch.Delete(entity));
                    break;
                }
                catch (StorageException e)
                {
                    if (successIfNotExist && e.RequestInformation.HttpStatusCode == 404)
                    {
                        continue;
                    }

                    if (e.RequestInformation.HttpStatusCode == 400 || e.RequestInformation.HttpStatusCode == 412)
                    {
                        if (i + 1 == 3)
                        {
                            throw;
                        }

                        if (retryOnConfliction)
                        {
                            await Task.Delay(100);
                            continue;
                        }
                    }
                    throw;
                }
            }
        }

        public static async Task<TableQueryResult<T>> WhereAsync<T>(this CloudTable table,
            string filter = null, TableContinuationToken continuationToken = null, int takeCount = -1, IList<string> fields = null)
            where T : ITableEntity, new()
        {
            var query = new TableQuery<T>();
            if (filter != null)
            {
                query = query.Where(filter);
            }
            if (fields != null && fields.Count > 0)
            {
                query = query.Select(fields);
            }
            if (takeCount > 0 && takeCount <= 1000)
            {
                query = query.Take(takeCount);
            }
            if (continuationToken == null)
            {
                var result = new List<T>();
                do
                {
                    var queryResult = await table.ExecuteQuerySegmentedAsync(query, continuationToken);
                    result.AddRange(queryResult.Results);
                    continuationToken = queryResult.ContinuationToken;
                    if (takeCount > 0 && result.Count >= takeCount)
                    {
                        break;
                    }
                } while (continuationToken != null);
                return new TableQueryResult<T>(result, null);
            }
            else
            {
                var queryResult = await table.ExecuteQuerySegmentedAsync(query, continuationToken);
                return new TableQueryResult<T>(queryResult.Results, queryResult.ContinuationToken);
            }
        }

        public static async Task<T> SingleOrDefaultAsync<T>(this CloudTable table, string filter = null) where T : ITableEntity, new()
        {
            var list = await table.WhereAsync<T>(filter, null, 1);
            return list.SingleOrDefault();
        }

        public static async Task<T> FirstOrDefaultAsync<T>(this CloudTable table, string filter = null) where T : ITableEntity, new()
        {
            var list = await table.WhereAsync<T>(filter, null, 1);
            return list.FirstOrDefault();
        }

        public static async Task<T> RetrieveAsync<T>(this CloudTable table, string partitionKey = null, string rowKey = null) where T : ITableEntity, new()
        {
            if (partitionKey != null && rowKey != null)
            {
                var query = await table.ExecuteAsync(TableOperation.Retrieve<T>(partitionKey, rowKey));
                if (query.Result == null)
                {
                    return default;
                }
                return (T)query.Result;
            }
            else
            {
                string filter = null;
                if (partitionKey != null)
                {
                    filter = TableQuery.GenerateFilterCondition(nameof(ITableEntity.PartitionKey), QueryComparisons.Equal, partitionKey);
                }
                else if (rowKey != null)
                {
                    filter = TableQuery.GenerateFilterCondition(nameof(ITableEntity.RowKey), QueryComparisons.Equal, rowKey);
                }

                return await table.FirstOrDefaultAsync<T>(filter);
            }
        }

        private static async Task ExecuteBatchAsync<T>(this CloudTable table, IEnumerable<T> entities, Action<TableBatchOperation, T> operation) where T : ITableEntity, new()
        {
            var groups = entities.GroupBy(r => r.PartitionKey);
            TableBatchOperation batchOperation;
            foreach (var group in groups)
            {
                batchOperation = new TableBatchOperation();
                foreach (var entity in group)
                {
                    operation(batchOperation, entity);
                    if (batchOperation.Count >= 100)
                    {
                        try
                        {
                            await table.ExecuteBatchAsync(batchOperation);
                            batchOperation = new TableBatchOperation();
                        }
                        catch (StorageException e)
                        {
                            var ri = e.RequestInformation;
                            var logger = Logger.Instance;
                            if (ri != null && logger != null)
                            {
                                logger.LogError($"An Error occurred with status code {ri.HttpStatusCode} while making changes to storage: {ri.HttpStatusMessage}");
                            }
                            throw;
                        }
                    }
                }
                if (batchOperation.Count == 1)
                {
                    var singleOperation = batchOperation.Single();
                    try
                    {
                        await table.ExecuteAsync(singleOperation);
                    }
                    catch (StorageException e)
                    {
                        var ri = e.RequestInformation;
                        var logger = Logger.Instance;
                        if (ri != null && logger != null)
                        {
                            logger.LogError($"An Error occurred with status code {ri.HttpStatusCode} while making changes to storage: {ri.HttpStatusMessage}");
                        }
                        throw;
                    }
                }
                else if (batchOperation.Count > 0)
                {
                    try
                    {
                        await table.ExecuteBatchAsync(batchOperation);
                    }
                    catch (StorageException e)
                    {
                        var ri = e.RequestInformation;
                        var logger = Logger.Instance;
                        if (ri != null && logger != null)
                        {
                            logger.LogError($"An Error occurred with status code {ri.HttpStatusCode} while making changes to storage: {ri.HttpStatusMessage}");
                        }
                        throw;
                    }
                }
            }
        }
    }
}
