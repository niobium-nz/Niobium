using System.Threading.Tasks;

namespace Cod.Contract
{
    public interface IEventHandler<T>
    {
        Task HandleAsync(T sender, object e);
    }
}
