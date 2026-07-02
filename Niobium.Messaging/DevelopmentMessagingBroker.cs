using Microsoft.Extensions.Logging;

namespace Niobium.Messaging
{
    internal sealed class DevelopmentMessagingBroker<T>(ILogger<DevelopmentMessagingBroker<T>> logger) : IMessagingBroker<T>
    {
        public Task<MessagingEntry<T>?> DequeueAsync(TimeSpan? maxWaitTime = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<MessagingEntry<T>?>(null);
        }

        public Task<IEnumerable<MessagingEntry<T>>> DequeueAsync(int limit, TimeSpan? maxWaitTime = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Enumerable.Empty<MessagingEntry<T>>());
        }

        public Task EnqueueAsync(IEnumerable<MessagingEntry<T>> messages, CancellationToken cancellationToken = default)
        {
#if DEBUG
            foreach (MessagingEntry<T> message in messages)
            {
#pragma warning disable CA1873 // Avoid potentially expensive logging
                logger.LogInformation($"[DevelopmentMessagingBroker] Enqueued message: {JsonMarshaller.Marshall(message)}");
#pragma warning restore CA1873 // Avoid potentially expensive logging
            }
#endif
            return Task.CompletedTask;
        }

        public Task<IEnumerable<MessagingEntry<T>>> PeekAsync(int? limit, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Enumerable.Empty<MessagingEntry<T>>());
        }
    }
}
