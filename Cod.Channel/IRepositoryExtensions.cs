using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cod.Channel
{
    public static class IRepositoryExtensions
    {
        public static async Task<OperationResult<IReadOnlyCollection<TDomain>>> LoadByPrefixAsync<TDomain, TEntity>(
            this IRepository<TDomain, TEntity> repository,
            string partitionKeyPrefix,
            int count = -1,
            bool force = false,
            bool continueToLoadMore = false)
            where TDomain : IChannelDomain<TEntity>
            where TEntity : IEntity
            => await repository.LoadAsync(partitionKeyPrefix, $"{partitionKeyPrefix}~", count, force, continueToLoadMore);
    }
}
