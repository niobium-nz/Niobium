namespace Cod.Messaging
{
    public static class IMessagingBrokerExtensions
    {
        public static async Task EnqueueAsync<T>(this IMessagingBroker<T> broker, MessagingEntry<T> message)
        {
            await broker.EnqueueAsync(new[] { message });
        }
    }
}
