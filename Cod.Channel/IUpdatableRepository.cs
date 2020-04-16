using System.Threading.Tasks;

namespace Cod.Channel
{
    public interface IUpdatableRepository<TDomain, TEntity, TUpdateParams> : IRepository<TDomain, TEntity>
        where TDomain : IChannelDomain<TEntity>
    {
        Task<OperationResult<TDomain>> UpdateAsync(TUpdateParams parameter);
    }
}
