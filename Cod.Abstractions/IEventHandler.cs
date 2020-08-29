using System.Threading.Tasks;

namespace Cod
{
    public interface IEventHandler<T>
    {
        Task HandleAsync(T sender, object e);
    }
}
