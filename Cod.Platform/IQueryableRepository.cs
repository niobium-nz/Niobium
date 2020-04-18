using System.Threading.Tasks;

namespace Cod.Platform
{
    public interface IQueryableRepository<T> : IRepository<T>
    {
        Task<TableQueryResult<T>> GetAsync(string partitionKey, string rowKeyStart, string rowKeyEnd, int limit = -1);
    }
}
