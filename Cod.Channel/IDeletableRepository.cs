using System.Threading.Tasks;

namespace Cod.Channel
{
    public interface IDeletableRepository<TDomain, TEntity> : IRepository<TDomain, TEntity>
        where TDomain : IChannelDomain<TEntity>
        where TEntity : IEntity
    {
        Task<OperationResult> DeleteAsync(StorageKey key);
    }
}
