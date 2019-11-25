using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cod.Platform
{
    public interface IRepository<T>
    {
        Task<IEnumerable<T>> CreateAsync(IEnumerable<T> entities, bool replaceIfExist);

        Task<IEnumerable<T>> UpdateAsync(IEnumerable<T> entities);

        Task<IEnumerable<T>> DeleteAsync(IEnumerable<T> entities);

        Task<TableQueryResult<T>> GetAsync(int limit);

        Task<TableQueryResult<T>> GetAsync(string partitionKey, int limit);

        Task<T> GetAsync(string partitionKey, string rowKey);
    }
}
