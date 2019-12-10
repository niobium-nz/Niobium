using System.Threading.Tasks;

namespace Cod.Contract
{
    public static class IEventHandlerExtensions
    {
        public static async Task HandleAsync<T>(this IEventHandler<T> eventHandler, T sender) => await eventHandler.HandleAsync(sender, null);
    }
}
