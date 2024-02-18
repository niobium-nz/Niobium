using Cod.Model;
using System.Threading.Tasks;

namespace Cod
{
    public static class IQueueExtensions
    {
        public static async Task EnqueueAsync(this IQueue queue, QueueMessage entity)
        {
            await queue.EnqueueAsync(new[] { entity });
        }
    }
}
