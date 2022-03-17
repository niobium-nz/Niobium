using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cod.Channel
{
    public interface IRepository<TDomain, TEntity>
        where TDomain : IChannelDomain<TEntity>
        where TEntity : IEntity
    {
        IReadOnlyCollection<TDomain> Data { get; }

        Task<OperationResult<TDomain>> LoadAsync(string partitionKey, string rowKey, bool force = false);

        Task<OperationResult<IReadOnlyCollection<TDomain>>> LoadAsync(string partitionKey, int count = -1, bool force = false, bool continueToLoadMore = false);

        Task<OperationResult<IReadOnlyCollection<TDomain>>> LoadAsync(string partitionKey, string rowKeyStart, string rowKeyEnd, int count = -1, bool force = false, bool continueToLoadMore = false);

        Task<OperationResult<IReadOnlyCollection<TDomain>>> LoadAsync(string partitionKeyStart, string partitionKeyEnd, int count = -1, bool force = false, bool continueToLoadMore = false);

        Task<OperationResult<IReadOnlyCollection<TDomain>>> LoadAsync(string partitionKeyStart, string partitionKeyEnd, string rowKeyStart, string rowKeyEnd, int count = -1, bool force = false, bool continueToLoadMore = false);

        Task<OperationResult<IReadOnlyCollection<TDomain>>> LoadAsync(int count = -1, bool force = false, bool continueToLoadMore = false);

        void Uncache(string partitionKey, string rowKey);

        void Uncache(TDomain domainObject);

        void Uncache(IEnumerable<TDomain> domainObjects);

        void Uncache(TEntity entity);

        void Uncache(IEnumerable<TEntity> entities);
    }
}
