using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cod.Platform
{
    public interface IQueryableRepository<T> : IRepository<T>
    {
        Task<TableQueryResult<T>> GetAsync(string partitionKey, IList<string> fields, int limit = -1);

        Task<TableQueryResult<T>> GetAsync(string partitionKey, string rowKeyStart, string rowKeyEnd, int limit = -1);

        Task<TableQueryResult<T>> GetAsync(string partitionKey, string rowKeyStart, string rowKeyEnd, IList<string> fields, int limit = -1);

        Task<TableQueryResult<T>> GetAsync(string partitionKeyStart, string partitionKeyEnd, IList<string> fields, int limit = -1);

        Task<TableQueryResult<T>> GetAsync(string partitionKeyStart, string partitionKeyEnd, string rowKeyStart, string rowKeyEnd, int limit = -1);

        Task<TableQueryResult<T>> GetAsync(string partitionKeyStart, string partitionKeyEnd, string rowKeyStart, string rowKeyEnd, IList<string> fields, int limit = -1);
    }
}
