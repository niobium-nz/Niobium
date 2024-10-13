using Azure;
using Azure.Data.Tables;
using Cod.Platform;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.RegularExpressions;

namespace Cod.Table.StorageAccount
{
    public class CloudTableRepository<T> : IRepository<T>, IQueryableRepository<T> where T : class, new()
    {
        private const string And = " and ";
        private const string Equal = "eq";
        private const string GreaterThanOrEqual = "ge";
        private const string LessThanOrEqual = "le";
        private static readonly string etagKey = EntityKeyKind.ETag.ToString();
        private static readonly List<T> emptyList = new();
        private static readonly EntityBag<T> emptyResult = new(emptyList, null);
        private static readonly Regex keysRegex = new("(http|https)\\:\\/\\/.*\\(PartitionKey='([^']+)',RowKey='([^']+)'\\)");
        private readonly ILogger logger;

        public string TableName { get; set; }

        protected TableServiceClient Client { get; }

        public CloudTableRepository(TableServiceClient client, ILogger<CloudTableRepository<T>> logger)
        {
            Client = client;
            this.logger = logger;
        }

        public async Task<T> RetrieveAsync(string partitionKey, string rowKey, IList<string> fields = null, CancellationToken cancellationToken = default)
        {
            try
            {
                NullableResponse<EntityDictionary> response = await GetTable().GetEntityIfExistsAsync<EntityDictionary>(partitionKey, rowKey, select: fields, cancellationToken: cancellationToken);

                if (response.HasValue)
                {
                    return response.Value.FromTableEntity<T>();
                }
                else
                {
                    return null;
                }
            }
            catch (RequestFailedException e)
            {
                string errorMessage = $"An Error occurred with status code {e.Status} while retrieving entity {partitionKey} -> {rowKey}: {e.Message}";
                logger?.LogWarning(errorMessage);
                throw new HttpRequestException(errorMessage, inner: e, statusCode: (HttpStatusCode)e.Status);
            }
        }

        public async Task<IEnumerable<T>> CreateAsync(IEnumerable<T> entities, bool replaceIfExist = false, DateTimeOffset? expiry = null, CancellationToken cancellationToken = default)
        {
            if (entities is null)
            {
                throw new ArgumentNullException(nameof(entities));
            }

            if (!entities.Any())
            {
                return entities;
            }

            if (expiry.HasValue)
            {
                throw new NotSupportedException($"Setting of '{nameof(expiry)}' is not supported on Azure Storage Table.");
            }

            DateTimeOffset now = DateTimeOffset.UtcNow;
            List<EntityDictionary> tableEntities = new();
            foreach (T entity in entities)
            {
                if (entity is ITrackable trackable && !trackable.Created.HasValue)
                {
                    trackable.Created = now;
                }

                tableEntities.Add(DBEntityHelper.ToTableEntity(entity));
            }

            TableClient table = GetTable();
            IEnumerable<EntityDictionary> result = replaceIfExist
                ? await ExecuteBatchWithRetryAsync(
                    tableEntities,
                    async (entity, token) => await table.UpsertEntityAsync(entity, cancellationToken: cancellationToken),
                    entity => new TableTransactionAction(TableTransactionActionType.UpsertReplace, entity),
                    cancellationToken: cancellationToken)
                : await ExecuteBatchWithRetryAsync(
                    tableEntities,
                    async (entity, token) => await table.AddEntityAsync(entity, cancellationToken: cancellationToken),
                    entity => new TableTransactionAction(TableTransactionActionType.Add, entity),
                    cancellationToken: cancellationToken);
            return result.Select(r => r.FromTableEntity<T>());
        }

        public async Task<IEnumerable<T>> UpdateAsync(IEnumerable<T> entities, bool preconditionCheck = true, CancellationToken cancellationToken = default)
        {
            if (entities is null)
            {
                throw new ArgumentNullException(nameof(entities));
            }

            if (!entities.Any())
            {
                return entities;
            }

            IEnumerable<EntityDictionary> tableEntities = ToTableEntities(entities, preconditionCheck);
            TableClient table = GetTable();

            IEnumerable<EntityDictionary> result = await ExecuteBatchWithRetryAsync(
                tableEntities,
                async (entity, token) => await table.UpdateEntityAsync(
                    entity,
                    ifMatch: entity.ETag,
                    mode: TableUpdateMode.Replace,
                    cancellationToken: cancellationToken),
                entity => new TableTransactionAction(
                    TableTransactionActionType.UpdateReplace,
                    entity,
                    etag: entity.ETag),
                cancellationToken: cancellationToken);
            return result.Select(r => r.FromTableEntity<T>());
        }

        public async Task DeleteAsync(IEnumerable<T> entities, bool preconditionCheck = true, bool successIfNotExist = true, CancellationToken cancellationToken = default)
        {
            if (entities is null)
            {
                throw new ArgumentNullException(nameof(entities));
            }

            if (!entities.Any())
            {
                return;
            }

            IEnumerable<EntityDictionary> tableEntities = ToTableEntities(entities, preconditionCheck);
            TableClient table = GetTable();

            await ExecuteBatchWithRetryAsync(
                tableEntities,
                async (entity, token) => await table.DeleteEntityAsync(
                    entity.PartitionKey,
                    entity.RowKey,
                    ifMatch: entity.ETag,
                    cancellationToken: cancellationToken),
                entity => new TableTransactionAction(
                    TableTransactionActionType.Delete,
                    entity,
                    etag: entity.ETag),
                errorSuppressed: successIfNotExist ? new[] { HttpStatusCode.NotFound } : null,
                cancellationToken: cancellationToken);
        }

        public async Task<EntityBag<T>> GetAsync(int limit, string continuationToken = null, IList<string> fields = null, CancellationToken cancellationToken = default)
        {
            return await QueryAsync(null, fields: fields, limit: limit, continuationToken: continuationToken, cancellationToken: cancellationToken);
        }

        public async Task<EntityBag<T>> GetAsync(string partitionKey, int limit, string continuationToken = null, IList<string> fields = null, CancellationToken cancellationToken = default)
        {
            return await QueryAsync(filter: $"{nameof(ITableEntity.PartitionKey)} {Equal} '{partitionKey}'", limit: limit, fields: fields, continuationToken: continuationToken, cancellationToken: cancellationToken);
        }

        public IAsyncEnumerable<T> GetAsync(IList<string> fields = null, CancellationToken cancellationToken = default)
        {
            return QueryAsync(null, fields: fields, cancellationToken: cancellationToken);
        }

        public IAsyncEnumerable<T> GetAsync(string partitionKey, IList<string> fields = null, CancellationToken cancellationToken = default)
        {
            return QueryAsync(filter: $"{nameof(ITableEntity.PartitionKey)} {Equal} '{partitionKey}'", fields: fields, cancellationToken: cancellationToken);
        }

        public async Task<EntityBag<T>> QueryAsync(string partitionKey, string rowKeyStart, string rowKeyEnd, int limit, IList<string> fields = null, string continuationToken = null, CancellationToken cancellationToken = default)
        {
            return await QueryAsync(filter: string.Join(And, new[]
                      {
                  $"{nameof(ITableEntity.PartitionKey)} {Equal} '{partitionKey}'",
                  $"{nameof(ITableEntity.RowKey)} {GreaterThanOrEqual} '{rowKeyStart}'",
                  $"{nameof(ITableEntity.RowKey)} {LessThanOrEqual} '{rowKeyEnd}'",
              }), fields: fields, limit: limit, continuationToken: continuationToken, cancellationToken: cancellationToken);
        }

        public async Task<EntityBag<T>> QueryAsync(string partitionKeyStart, string partitionKeyEnd, int limit, IList<string> fields = null, string continuationToken = null, CancellationToken cancellationToken = default)
        {
            return await QueryAsync(filter: string.Join(And, new[]
                      {
                  $"{nameof(ITableEntity.PartitionKey)} {GreaterThanOrEqual} '{partitionKeyStart}'",
                  $"{nameof(ITableEntity.PartitionKey)} {LessThanOrEqual} '{partitionKeyEnd}'",
              }), fields: fields, limit: limit, continuationToken: continuationToken, cancellationToken: cancellationToken);
        }

        public async Task<EntityBag<T>> QueryAsync(string partitionKeyStart, string partitionKeyEnd, string rowKeyStart, string rowKeyEnd, int limit, IList<string> fields = null, string continuationToken = null, CancellationToken cancellationToken = default)
        {
            return await QueryAsync(filter: string.Join(And, new[]
                      {
                  $"{nameof(ITableEntity.PartitionKey)} {GreaterThanOrEqual} '{partitionKeyStart}'",
                  $"{nameof(ITableEntity.PartitionKey)} {LessThanOrEqual} '{partitionKeyEnd}'",
                  $"{nameof(ITableEntity.RowKey)} {GreaterThanOrEqual} '{rowKeyStart}'",
                  $"{nameof(ITableEntity.RowKey)} {LessThanOrEqual} '{rowKeyEnd}'",
              }), fields: fields, limit: limit, continuationToken: continuationToken, cancellationToken: cancellationToken);
        }

        public IAsyncEnumerable<T> QueryAsync(string partitionKey, string rowKeyStart, string rowKeyEnd, IList<string> fields = null, CancellationToken cancellationToken = default)
        {
            return QueryAsync(filter: string.Join(And, new[]
                      {
                  $"{nameof(ITableEntity.PartitionKey)} {Equal} '{partitionKey}'",
                  $"{nameof(ITableEntity.RowKey)} {GreaterThanOrEqual} '{rowKeyStart}'",
                  $"{nameof(ITableEntity.RowKey)} {LessThanOrEqual} '{rowKeyEnd}'",
              }), fields: fields, cancellationToken: cancellationToken);
        }

        public IAsyncEnumerable<T> QueryAsync(string partitionKeyStart, string partitionKeyEnd, IList<string> fields = null, CancellationToken cancellationToken = default)
        {
            return QueryAsync(filter: string.Join(And, new[]
                      {
                  $"{nameof(ITableEntity.PartitionKey)} {GreaterThanOrEqual} '{partitionKeyStart}'",
                  $"{nameof(ITableEntity.PartitionKey)} {LessThanOrEqual} '{partitionKeyEnd}'",
              }), fields: fields, cancellationToken: cancellationToken);
        }

        public IAsyncEnumerable<T> QueryAsync(string partitionKeyStart, string partitionKeyEnd, string rowKeyStart, string rowKeyEnd, IList<string> fields = null, CancellationToken cancellationToken = default)
        {
            return QueryAsync(filter: string.Join(And, new[]
                      {
                  $"{nameof(ITableEntity.PartitionKey)} {GreaterThanOrEqual} '{partitionKeyStart}'",
                  $"{nameof(ITableEntity.PartitionKey)} {LessThanOrEqual} '{partitionKeyEnd}'",
                  $"{nameof(ITableEntity.RowKey)} {GreaterThanOrEqual} '{rowKeyStart}'",
                  $"{nameof(ITableEntity.RowKey)} {LessThanOrEqual} '{rowKeyEnd}'",
              }), fields: fields, cancellationToken: cancellationToken);
        }

        protected virtual async Task<EntityBag<T>> QueryAsync(string filter, int limit, IList<string> fields = null, string continuationToken = null, CancellationToken cancellationToken = default)
        {
            int? maxPerPage = FigureMaxQueryPageSize(limit);
            IAsyncEnumerable<Page<EntityDictionary>> pages = GetTable().QueryAsync<EntityDictionary>(
                    filter: filter,
                    maxPerPage: maxPerPage,
                    select: fields,
                    cancellationToken: cancellationToken)
                .AsPages(continuationToken: continuationToken, pageSizeHint: maxPerPage);

            await using IAsyncEnumerator<Page<EntityDictionary>> enumerator = pages.GetAsyncEnumerator(cancellationToken);
            Page<EntityDictionary> firstPage = await enumerator.MoveNextAsync() ? enumerator.Current : default;
            if (firstPage == null || firstPage.Values.Count == 0)
            {
                return emptyResult;
            }

            List<T> result = firstPage.Values.Select(r => r.FromTableEntity<T>()).ToList();
            return new EntityBag<T>(result, firstPage.ContinuationToken);
        }

        protected virtual IAsyncEnumerable<T> QueryAsync(string filter, IList<string> fields = null, CancellationToken cancellationToken = default)
        {
            AsyncPageable<EntityDictionary> result = GetTable().QueryAsync<EntityDictionary>(
                filter: filter,
                select: fields,
                cancellationToken: cancellationToken);
            return result.Select(r => r.FromTableEntity<T>());
        }

        protected virtual async Task<IEnumerable<TEntity>> ExecuteBatchWithRetryAsync<TEntity>(
            IEnumerable<TEntity> entities,
            Func<TEntity, CancellationToken, Task<Response>> singleOperation,
            Func<TEntity, TableTransactionAction> batchOperation,
            IEnumerable<HttpStatusCode> errorSuppressed = null,
            CancellationToken cancellationToken = default)
            where TEntity : ITableEntity
        {
            _ = entities ?? throw new ArgumentNullException(nameof(entities));
            int count = entities.Count();

            List<Response> responses;

            if (count == 1)
            {
                TEntity entity = entities.Single();
                Response response = await singleOperation(entity, cancellationToken);

                if (response.Headers.ETag.HasValue)
                {
                    entity.ETag = response.Headers.ETag.Value;
                }
                responses = new List<Response>() { response };
            }
            else
            {
                responses = count > 1
                    ? await ExecuteBatchAsync(entities, batchOperation, cancellationToken: cancellationToken)
                    : throw new ArgumentNullException(nameof(entities));
            }

            List<HttpRequestException> exceptions = null;
            foreach (Response response in responses)
            {
                if (response.IsError)
                {
                    HttpStatusCode errorCode = (HttpStatusCode)response.Status;
                    if (errorSuppressed == null || !errorSuppressed.Contains(errorCode))
                    {
                        string errorMessage = $"An Error occurred with status code {response.Status} while making changes to storage: {response.Content}";
                        logger?.LogError(errorMessage);
                        exceptions ??= new();
                        exceptions.Add(new HttpRequestException(errorMessage, inner: null, statusCode: errorCode));
                    }
                }
            }

            if (exceptions != null)
            {
                if (exceptions.Count == 1)
                {
                    throw exceptions.Single();
                }
                else if (exceptions.Count > 1)
                {
                    throw new AggregateException($"Error(s) occurred while executing transaction(s) on table: {typeof(T).Name}.", exceptions);
                }
            }

            return entities;
        }

        protected virtual async Task<List<Response>> ExecuteBatchAsync<TEntity>(
            IEnumerable<TEntity> entities,
            Func<TEntity, TableTransactionAction> createOperation,
            CancellationToken cancellationToken = default)
            where TEntity : ITableEntity
        {
            List<List<TableTransactionAction>> batchOperationGroups = new();
            IEnumerable<IGrouping<string, TEntity>> groups = entities.GroupBy(r => r.PartitionKey);
            foreach (IGrouping<string, TEntity> group in groups)
            {
                List<TableTransactionAction> batchOperations = new();

                foreach (TEntity entity in group)
                {
                    TableTransactionAction batchOperation = createOperation(entity);
                    batchOperations.Add(batchOperation);

                    if (batchOperations.Count >= 100)
                    {
                        batchOperationGroups.Add(batchOperations);
                        batchOperations = new List<TableTransactionAction>();
                    }
                }

                if (batchOperations.Any())
                {
                    batchOperationGroups.Add(batchOperations);
                }
            }

            TableClient table = GetTable();
            List<Response> result = new();
            foreach (List<TableTransactionAction> batchOperations in batchOperationGroups)
            {
                if (batchOperations.Count > 0)
                {
                    Response<IReadOnlyList<Response>> response = await table.SubmitTransactionAsync(batchOperations, cancellationToken: cancellationToken);

                    List<ITableEntity> batchedEntities = batchOperations.Select(o => o.Entity).ToList();
                    for (int i = 0; i < response.Value.Count; i++)
                    {
                        SetEtagByResponse(response.Value[i], i, batchedEntities);
                    }

                    result.AddRange(response.Value);
                }
            }

            return result;
        }

        protected virtual int? FigureMaxQueryPageSize(int limitRequested)
        {
            return limitRequested <= 0 ? 1000 : limitRequested;
        }

        protected virtual TableClient GetTable()
        {
            if (string.IsNullOrEmpty(TableName))
            {
                TableName = typeof(T).Name;
            }

            return Client.GetTableClient(TableName);
        }

        private static IEnumerable<EntityDictionary> ToTableEntities(IEnumerable<T> entities, bool preconditionCheck)
        {
            List<EntityDictionary> tableEntities = new();
            foreach (T entity in entities)
            {
                EntityDictionary tableEntity = DBEntityHelper.ToTableEntity(entity);
                if (!preconditionCheck)
                {
                    tableEntity.ETag = ETag.All;
                }

                tableEntities.Add(tableEntity);
            }

            return tableEntities;
        }

        private static void SetEtagByResponse(Response response, int responseIndex, List<ITableEntity> entities)
        {
            if (!response.IsError && response.Headers.ETag.HasValue)
            {
                ITableEntity target = response.Headers.TryGetValue("Location", out string location) && TryParseKeys(location, out string pk, out string rk)
                    ? entities.Single(e => e.PartitionKey == pk && e.RowKey == rk)
                    : entities[responseIndex];
                target.ETag = response.Headers.ETag.Value;
            }
        }

        private static bool TryParseKeys(string location, out string partitionKey, out string rowKey)
        {
            Match match = keysRegex.Match(location);
            if (match.Success && match.Groups.Count == 4)
            {
                partitionKey = match.Groups[2].Value;
                rowKey = match.Groups[3].Value;
                return true;
            }

            partitionKey = null;
            rowKey = null;
            return false;
        }
    }
}
