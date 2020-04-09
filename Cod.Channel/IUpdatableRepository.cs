using System.Threading.Tasks;

namespace Cod.Channel
{
    public interface IUpdatableRepository<TDomain, TEntity> : IRepository<TDomain, TEntity>
        where TDomain : IChannelDomain<TEntity>
    {
        Task<OperationResult<TDomain>> UpdateAsync(TEntity entity);
    }
}
