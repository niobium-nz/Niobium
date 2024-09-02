namespace Cod.Storage.Messaging
{
    public static class IQueueExtensions
    {
        public static async Task EnqueueAsync(this IMessagingHub queue, QueueMessage entity)
        {
            await queue.EnqueueAsync(new[] { entity });
        }
    }
}
