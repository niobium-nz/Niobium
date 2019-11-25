using System.Threading.Tasks;

namespace Cod.Platform
{
    public interface IEventHandler<TEntity>
    {
        Task HandleAsync(IDomain<TEntity> sender, object e);
    }
}
