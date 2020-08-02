using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Storage.Queue;
using Newtonsoft.Json;

namespace Cod.Platform
{
    internal class PlatformQueue : IQueue
    {
        public async Task<OperationResult> EnqueueAsync(IEnumerable<QueueMessage> entities)
        {
            foreach (var item in entities)
            {
                var queue = CloudStorage.GetQueue(item.PartitionKey);
                await queue.CreateIfNotExistsAsync();
                var msg = item.Body is string str ? str : JsonConvert.SerializeObject(item.Body);
                if (item.Delay.HasValue)
                {
                    await queue.AddMessageAsync(new CloudQueueMessage(msg), null, item.Delay.Value, null, null);
                }
                else
                {
                    await queue.AddMessageAsync(new CloudQueueMessage(msg));
                }
            }

            return OperationResult.Create();
        }

        public async Task<IEnumerable<QueueMessage>> PeekAsync(string queueName, int limit)
            => (await CloudStorage.GetQueue(queueName).PeekMessagesAsync(limit)).Select(m => new QueueMessage
            {
                Body = m.AsString,
                PartitionKey = queueName,
                RowKey = m.Id
            });
    }
}
