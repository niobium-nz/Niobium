using Azure;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;

namespace Cod.Messaging.StorageAccount
{
    public class StorageAccountQueueBroker<T>(QueueServiceClient client, string queueName) : IMessagingBroker<T> where T : class, new()
    {
        public StorageAccountQueueBroker(QueueServiceClient client) : this(client, typeof(T).Name.ToLowerInvariant())
        {
        }

        protected QueueServiceClient Client { get; } = client;

        protected virtual string QueueName { get; } = queueName;

        protected virtual bool CreateQueueIfNotExist { get; } = true;

        public virtual async Task<MessagingEntry<T>?> DequeueAsync(TimeSpan? maxWaitTime = default, CancellationToken cancellationToken = default)
        {
            QueueClient q = await GetQueueAsync(cancellationToken);
            Response<QueueMessage> msg = await q.ReceiveMessageAsync(cancellationToken: cancellationToken);
            return msg != null && msg.Value != null
                ? new StorageQueueMessage<T>(() => q.DeleteMessageAsync(msg.Value.MessageId, msg.Value.PopReceipt))
                {
                    ID = msg.Value.MessageId,
                    Body = msg.Value.MessageText,
                    Timestamp = msg.Value.InsertedOn,
                }
                : null;
        }

        public virtual async Task<IEnumerable<MessagingEntry<T>>> DequeueAsync(int limit, TimeSpan? maxWaitTime = default, CancellationToken cancellationToken = default)
        {
            List<MessagingEntry<T>> result = [];
            QueueClient q = await GetQueueAsync(cancellationToken);
            Response<QueueMessage[]> msgs = await q.ReceiveMessagesAsync(maxMessages: limit <= 0 ? null : limit, cancellationToken: cancellationToken);
            if (msgs.Value.Length != 0)
            {
                foreach (QueueMessage msg in msgs.Value)
                {
                    result.Add(new StorageQueueMessage<T>(
                        () => q.DeleteMessageAsync(msg.MessageId, msg.PopReceipt))
                    {
                        ID = msg.MessageId,
                        Body = msg.MessageText,
                        Timestamp = msg.InsertedOn,
                    });
                }

            }
            return result;
        }

        public virtual async Task EnqueueAsync(IEnumerable<MessagingEntry<T>> messages, CancellationToken cancellationToken = default)
        {
            QueueClient q = await GetQueueAsync(cancellationToken);
            foreach (MessagingEntry<T> message in messages)
            {
                TimeSpan? delay = message.Schedule == null ? null : message.Schedule - DateTimeOffset.UtcNow;
                if (delay.HasValue && delay.Value.TotalSeconds < 0)
                {
                    if (delay.Value.TotalDays > 7)
                    {
                        delay = TimeSpan.FromDays(7);
                    }
                    else if (delay.Value.TotalSeconds < 0)
                    {
                        delay = null;
                    }
                }

                await q.SendMessageAsync(message.Body, visibilityTimeout: delay, cancellationToken: cancellationToken);
            }
        }

        public virtual async Task<IEnumerable<MessagingEntry<T>>> PeekAsync(int? limit, CancellationToken cancellationToken = default)
        {
            QueueClient q = await GetQueueAsync(cancellationToken);
            Response<PeekedMessage[]> msgs = await q.PeekMessagesAsync(maxMessages: limit, cancellationToken: cancellationToken);
            return msgs.Value.Select(m => new MessagingEntry<T>
            {
                ID = m.MessageId,
                Body = m.MessageText,
                Timestamp = m.InsertedOn,
            });
        }

        protected virtual async Task<QueueClient> GetQueueAsync(CancellationToken cancellationToken)
        {
            QueueClient queue = Client.GetQueueClient(QueueName);
            if (CreateQueueIfNotExist)
            {
                await queue.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
            }

            return queue;
        }
    }
}
