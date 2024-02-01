using System.Threading.Tasks;
using Cod.Model;

namespace Cod
{
    public static class IQueueExtensions
    {
        public static async Task EnqueueAsync(this IQueue queue, QueueMessage entity)
            => await queue.EnqueueAsync(new[] { entity });
    }
}
