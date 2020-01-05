using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cod
{
    public static class IEventHandlerExtensions
    {
        public static async Task InvokeAsync<T>(this IEventHandler<T> eventHandler, T sender) => await eventHandler.HandleAsync(sender, null);

        public static async Task InvokeAsync<T>(this IEnumerable<IEventHandler<T>> handlers, T sender, object e)
        {
            foreach (var eventHandler in handlers)
            {
                await eventHandler.HandleAsync(sender, e);
            }
        }
    }
}
