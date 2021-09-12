using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cod.Model;
using Microsoft.Azure.Storage.Queue;

namespace Cod.Platform
{
    public class PlatformQueue : IQueue
    {
        public virtual async Task<DisposableQueueMessage> DequeueAsync(string queueName)
        {
            var q = CloudStorage.GetQueue(queueName);
            var msg = await q.GetMessageAsync();
            if (msg != null)
            {
                return new DisposableQueueMessage(
                    () => q.DeleteMessage(msg.Id, msg.PopReceipt),
                    () => q.DeleteMessageAsync(msg.Id, msg.PopReceipt))
                {
                    Body = msg.AsString,
                    PartitionKey = queueName,
                    RowKey = msg.Id
                };
            }
            else
            {
                return null;
            }
        }

        public virtual async Task<IEnumerable<DisposableQueueMessage>> DequeueAsync(string queueName, int limit)
        {
            var result = new List<DisposableQueueMessage>();
            var q = CloudStorage.GetQueue(queueName);
            var msgs = await q.GetMessagesAsync(limit);
            if (msgs != null && msgs.Any())
            {
                foreach (var msg in msgs)
                {
                    result.Add(new DisposableQueueMessage(
                        () => q.DeleteMessage(msg.Id, msg.PopReceipt),
                        () => q.DeleteMessageAsync(msg.Id, msg.PopReceipt))
                    {
                        Body = msg.AsString,
                        PartitionKey = queueName,
                        RowKey = msg.Id
                    });
                }

            }
            return result;
        }


        public virtual async Task<OperationResult> EnqueueAsync(IEnumerable<QueueMessage> entities)
        {
            foreach (var item in entities)
            {
                var queue = CloudStorage.GetQueue(item.PartitionKey);
                await queue.CreateIfNotExistsAsync();
                var msg = item.Body is string str ? str : JsonSerializer.SerializeObject(item.Body);
                if (item.Delay.HasValue)
                {
                    await queue.AddMessageAsync(new CloudQueueMessage(msg), null, item.Delay.Value, null, null);
                }
                else
                {
                    await queue.AddMessageAsync(new CloudQueueMessage(msg));
                }
            }

            return OperationResult.Success;
        }

        public virtual async Task<IEnumerable<QueueMessage>> PeekAsync(string queueName, int limit)
            => (await CloudStorage.GetQueue(queueName).PeekMessagesAsync(limit)).Select(m => new QueueMessage
            {
                Body = m.AsString,
                PartitionKey = queueName,
                RowKey = m.Id
            });
    }
}
