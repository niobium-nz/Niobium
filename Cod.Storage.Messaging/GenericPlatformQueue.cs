namespace Cod.Storage.Messaging
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "ByDesign")]
    public abstract class GenericPlatformQueue
    {
        protected IMessagingHub Queue { get; private set; }

        protected virtual bool CreateIfNotExist => true;

        protected abstract string QueueName { get; }

        public GenericPlatformQueue(IMessagingHub queue)
        {
            Queue = queue;
        }

        public virtual async Task<DisposableQueueMessage> DequeueAsync(bool createIfNotExist = true, CancellationToken cancellationToken = default)
        {
            return await Queue.DequeueAsync(QueueName, createIfNotExist: createIfNotExist, cancellationToken: cancellationToken);
        }

        public virtual async Task<IEnumerable<DisposableQueueMessage>> DequeueAsync(int? limit = null, bool createIfNotExist = true, CancellationToken cancellationToken = default)
        {
            return await Queue.DequeueAsync(QueueName, limit ?? -1, createIfNotExist: createIfNotExist, cancellationToken: cancellationToken);
        }

        public virtual async Task EnqueueAsync(IEnumerable<QueueMessage> entities, bool createIfNotExist = true, CancellationToken cancellationToken = default)
        {
            await Queue.EnqueueAsync(entities, createIfNotExist: createIfNotExist, cancellationToken: cancellationToken);
        }

        public virtual async Task<IEnumerable<QueueMessage>> PeekAsync(int? limit, bool createIfNotExist = true, CancellationToken cancellationToken = default)
        {
            return await Queue.PeekAsync(QueueName, limit, createIfNotExist: createIfNotExist, cancellationToken: cancellationToken);
        }
    }
}
