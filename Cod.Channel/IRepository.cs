using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cod.Channel
{
    public interface IRepository<TDomain, TEntity>
        where TDomain : IChannelDomain<TEntity>
    {
        IReadOnlyCollection<TDomain> Data { get; }

        Task<ContinuationToken> LoadAsync(string partitionKey, string rowKey);

        Task<ContinuationToken> LoadAsync(string partitionKey, int count = -1);

        Task<ContinuationToken> LoadAsync(string partitionKey, string rowKeyStart, string rowKeyEnd, int count = -1);

        Task<ContinuationToken> LoadAsync(string partitionKeyStart, string partitionKeyEnd, int count = -1);

        Task<ContinuationToken> LoadAsync(string partitionKeyStart, string partitionKeyEnd, string rowKeyStart, string rowKeyEnd, int count = -1);

        Task<ContinuationToken> LoadAsync(int count = -1);
    }
}
