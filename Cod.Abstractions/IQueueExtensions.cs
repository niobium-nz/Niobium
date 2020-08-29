using System.Threading.Tasks;

namespace Cod
{
    public static class IQueueExtensions
    {
        public static async Task<OperationResult> EnqueueAsync(this IQueue queue, QueueMessage entity)
            => await queue.EnqueueAsync(new[] { entity });
    }
}
