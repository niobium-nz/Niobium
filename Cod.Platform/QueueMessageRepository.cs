using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cod.Platform.Model;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;

namespace Cod.Platform
{
    internal class QueueMessageRepository : IRepository<QueueMessage>
    {
        public async Task<IEnumerable<QueueMessage>> CreateAsync(IEnumerable<QueueMessage> entities, bool replaceIfExist)
        {
            foreach (var item in entities)
            {
                var queue = CloudStorage.GetQueue(item.PartitionKey);
                await queue.CreateIfNotExistsAsync();
                var msg = item.Body is string ? (string)item.Body : JsonConvert.SerializeObject(item.Body);
                if (item.Delay.HasValue)
                {
                    await queue.AddMessageAsync(new CloudQueueMessage(msg), null, item.Delay.Value, null, null);
                }
                else
                {
                    await queue.AddMessageAsync(new CloudQueueMessage(msg));
                }
            }
            return entities;
        }

        public Task<IEnumerable<QueueMessage>> DeleteAsync(IEnumerable<QueueMessage> entities) => throw new NotImplementedException();

        public async Task<TableQueryResult<QueueMessage>> GetAsync(string partitionKey, int limit)
            => new TableQueryResult<QueueMessage>((await CloudStorage.GetQueue(partitionKey).PeekMessagesAsync(limit)).Select(m => new QueueMessage
            {
                Body = m.AsString,
                PartitionKey = partitionKey,
                RowKey = m.Id
            }).ToList(), null);

        public Task<QueueMessage> GetAsync(string partitionKey, string rowKey) => throw new NotImplementedException();

        public Task<TableQueryResult<QueueMessage>> GetAsync(int limit) => throw new NotImplementedException();

        public Task<IEnumerable<QueueMessage>> UpdateAsync(IEnumerable<QueueMessage> entities) => throw new NotImplementedException();
    }
}
