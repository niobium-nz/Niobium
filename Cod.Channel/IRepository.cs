using System.Collections.Generic;
using System.Threading.Tasks;
using Cod.Contract;

namespace Cod.Channel
{
    public interface IRepository<T>
    {
        IReadOnlyCollection<IDomain<T>> Data { get; }

        Task<ContinuationToken> LoadAsync(string partitionKey, string rowKey);

        Task<ContinuationToken> LoadAsync(string partitionKey, int count = -1);

        Task<ContinuationToken> LoadAsync(string partitionKey, string rowKeyStart, string rowKeyEnd, int count = -1);

        Task<ContinuationToken> LoadAsync(string partitionKeyStart, string partitionKeyEnd, int count = -1);

        Task<ContinuationToken> LoadAsync(string partitionKeyStart, string partitionKeyEnd, string rowKeyStart, string rowKeyEnd, int count = -1);

        Task<ContinuationToken> LoadAsync(int count = -1);
    }
}
