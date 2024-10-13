namespace Cod.Messaging
{
    public interface IMessagingBroker<T>
    {
        Task EnqueueAsync(IEnumerable<MessagingEntry<T>> messages, CancellationToken cancellationToken = default);

        Task<IEnumerable<MessagingEntry<T>>> PeekAsync(int? limit, CancellationToken cancellationToken = default);

        Task<MessagingEntry<T>> DequeueAsync(CancellationToken cancellationToken = default);

        Task<IEnumerable<MessagingEntry<T>>> DequeueAsync(int limit, CancellationToken cancellationToken = default);
    }
}
