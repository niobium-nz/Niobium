using System.Threading.Tasks;

namespace Cod.Channel
{
    public static class IRepositoryExtensions
    {
        public static async Task<OperationResult<ContinuationToken>> LoadByPrefixAsync<TDomain, TEntity>(
            this IRepository<TDomain, TEntity> repository,
            string partitionKeyPrefix,
            int count = -1,
            bool force = false)
            where TDomain : IChannelDomain<TEntity>
            => await repository.LoadAsync(partitionKeyPrefix, $"{partitionKeyPrefix}z", count, force);
    }
}
