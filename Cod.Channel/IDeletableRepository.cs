using System.Threading.Tasks;

namespace Cod.Channel
{
    public interface IDeletableRepository<TDomain, TEntity> : IRepository<TDomain, TEntity>
        where TDomain : IChannelDomain<TEntity>
    {
        Task<OperationResult> DeleteAsync(StorageKey key);
    }
}
