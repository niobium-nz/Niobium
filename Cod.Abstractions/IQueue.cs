using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cod
{
    public interface IQueue
    {
        Task<OperationResult> EnqueueAsync(IEnumerable<QueueMessage> entities);

        Task<IEnumerable<QueueMessage>> PeekAsync(string queueName, int limit);
    }
}
