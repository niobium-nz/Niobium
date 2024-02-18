using Azure.Data.Tables;
using Microsoft.Extensions.Logging;

namespace Cod.Platform.Database
{
    public class CosmosDBTableRepository<T> : CloudTableRepository<T>, IRepository<T>, IQueryableRepository<T> where T : class, ITableEntity, IEntity, new()
    {
        public CosmosDBTableRepository(TableServiceClient client, ILogger logger) : base(client, logger)
        {
        }

        protected override async Task<TableQueryResult<T>> QueryAsync(string filter, int limit = -1, IList<string> fields = null, string continuationToken = null, CancellationToken cancellationToken = default)
        {
            TableQueryResult<T> result = await base.QueryAsync(filter, limit, fields, continuationToken, cancellationToken);
            List<T> orderedResult = result.OrderBy(e => ((IEntity)e).PartitionKey).ThenBy(e => ((IEntity)e).RowKey).ToList();
            return new TableQueryResult<T>(orderedResult, result.ContinuationToken);
        }

        protected override int? FigureMaxQueryPageSize(int limitRequested)
        {
            return limitRequested > 0
                ? throw new NotSupportedException("Paged query is not supported on Cosmos DB Table API due to the inconsistent order on query result.")
                : null;
        }
    }
}
