namespace Niobium.Messaging
{
    public static class IMessagingBrokerExtensions
    {
        public static async Task EnqueueAsync<T>(this IMessagingBroker<T> broker, MessagingEntry<T> message, CancellationToken cancellationToken = default)
        {
            await broker.EnqueueAsync(new[] { message }, cancellationToken);
        }
    }
}
