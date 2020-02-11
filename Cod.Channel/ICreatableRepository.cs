using System.Threading.Tasks;

namespace Cod.Channel
{
    public interface ICreatableRepository<TDomain, TEntity, TCreateParams> : IRepository<TDomain, TEntity>
        where TDomain : IChannelDomain<TEntity>
        where TCreateParams : class
    {
        Task<OperationResult<TDomain>> CreateAsync(TCreateParams parameters);
    }
}
