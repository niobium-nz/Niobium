using Cod.Model;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Cod
{
    public interface IQueue
    {
        Task EnqueueAsync(IEnumerable<QueueMessage> entities, bool createIfNotExist = true, CancellationToken cancellationToken = default);

        Task<IEnumerable<QueueMessage>> PeekAsync(string queueName, int? limit, bool createIfNotExist = true, CancellationToken cancellationToken = default);

        Task<DisposableQueueMessage> DequeueAsync(string queueName, bool createIfNotExist = true, CancellationToken cancellationToken = default);

        Task<IEnumerable<DisposableQueueMessage>> DequeueAsync(string queueName, int limit, bool createIfNotExist = true, CancellationToken cancellationToken = default);
    }
}
