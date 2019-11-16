using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Cod.Platform
{
    public interface IEventHandler<TEntity>
    {
        Task HandleAsync(IDomain<TEntity> sender, object e, ILogger logger);
    }
}
