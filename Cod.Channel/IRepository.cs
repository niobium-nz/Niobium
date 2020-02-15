using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cod.Channel
{
    public interface IRepository<TDomain, TEntity>
        where TDomain : IChannelDomain<TEntity>
    {
        IReadOnlyCollection<TDomain> Data { get; }

        Task<OperationResult<TDomain>> LoadAsync(string partitionKey, string rowKey, bool force = false);

        Task<OperationResult<ContinuationToken>> LoadAsync(string partitionKey, int count = -1, bool force = false);

        Task<OperationResult<ContinuationToken>> LoadAsync(string partitionKey, string rowKeyStart, string rowKeyEnd, int count = -1, bool force = false);

        Task<OperationResult<ContinuationToken>> LoadAsync(string partitionKeyStart, string partitionKeyEnd, int count = -1, bool force = false);

        Task<OperationResult<ContinuationToken>> LoadAsync(string partitionKeyStart, string partitionKeyEnd, string rowKeyStart, string rowKeyEnd, int count = -1, bool force = false);

        Task<OperationResult<ContinuationToken>> LoadAsync(int count = -1, bool force = false);
    }
}
