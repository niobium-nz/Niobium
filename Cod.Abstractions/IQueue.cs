using System.Collections.Generic;
using System.Threading.Tasks;
using Cod.Model;

namespace Cod
{
    public interface IQueue
    {
        Task<OperationResult> EnqueueAsync(IEnumerable<QueueMessage> entities);

        Task<IEnumerable<QueueMessage>> PeekAsync(string queueName, int limit);

        Task<QueueMessage> DequeueAsync(string queueName);
    }
}
