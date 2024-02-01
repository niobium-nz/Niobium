using Azure;
using Azure.Storage.Queues;
using Cod.Model;

namespace Cod.Platform.Integration.Azure
{
    internal class CloudPlatformQueue : IQueue
    {
        protected QueueServiceClient Client { get; private set; }

        public CloudPlatformQueue(QueueServiceClient client)
        {
            Client = client;
        }

        public virtual async Task<DisposableQueueMessage> DequeueAsync(string queueName, bool createIfNotExist = true, CancellationToken cancellationToken = default)
        {
            QueueClient q = await GetQueueAsync(queueName, createIfNotExist, cancellationToken);
            global::Azure.Response<global::Azure.Storage.Queues.Models.QueueMessage> msg = await q.ReceiveMessageAsync(cancellationToken: cancellationToken);
            return msg != null
                ? new DisposableQueueMessage(
                    () => q.DeleteMessageAsync(msg.Value.MessageId, msg.Value.PopReceipt))
                {
                    Body = msg.Value.MessageText,
                    PartitionKey = queueName,
                    RowKey = msg.Value.MessageId
                }
                : null;
        }

        public virtual async Task<IEnumerable<DisposableQueueMessage>> DequeueAsync(string queueName, int limit, bool createIfNotExist = true, CancellationToken cancellationToken = default)
        {
            List<DisposableQueueMessage> result = new();
            QueueClient q = await GetQueueAsync(queueName, createIfNotExist, cancellationToken);
            Response<global::Azure.Storage.Queues.Models.QueueMessage[]> msgs = await q.ReceiveMessagesAsync(maxMessages: limit <= 0 ? null : limit, cancellationToken: cancellationToken);
            if (msgs.Value.Any())
            {
                foreach (global::Azure.Storage.Queues.Models.QueueMessage msg in msgs.Value)
                {
                    result.Add(new DisposableQueueMessage(
                        () => q.DeleteMessageAsync(msg.MessageId, msg.PopReceipt))
                    {
                        Body = msg.MessageText,
                        PartitionKey = queueName,
                        RowKey = msg.MessageId
                    });
                }

            }
            return result;
        }


        public virtual async Task EnqueueAsync(IEnumerable<QueueMessage> entities, bool createIfNotExist = true, CancellationToken cancellationToken = default)
        {
            foreach (QueueMessage item in entities)
            {
                QueueClient q = await GetQueueAsync(item.PartitionKey, createIfNotExist, cancellationToken);
                string msg = item.Body is string str ? str : JsonSerializer.SerializeObject(item.Body);
                await q.SendMessageAsync(msg, visibilityTimeout: item.Delay, cancellationToken: cancellationToken);
            }
        }

        public virtual async Task<IEnumerable<QueueMessage>> PeekAsync(string queueName, int? limit, bool createIfNotExist = true, CancellationToken cancellationToken = default)
        {
            QueueClient q = await GetQueueAsync(queueName, createIfNotExist, cancellationToken);
            global::Azure.Response<global::Azure.Storage.Queues.Models.PeekedMessage[]> msgs = await q.PeekMessagesAsync(maxMessages: limit, cancellationToken: cancellationToken);
            return msgs.Value.Select(m => new QueueMessage
            {
                Body = m.MessageText,
                PartitionKey = queueName,
                RowKey = m.MessageId
            });
        }

        protected async Task<QueueClient> GetQueueAsync(string queueName, bool createIfNotExist, CancellationToken cancellationToken)
        {
            QueueClient queue = Client.GetQueueClient(queueName);
            if (createIfNotExist)
            {
                await queue.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
            }

            return queue;
        }
    }
}
