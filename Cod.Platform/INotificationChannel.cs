using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cod.Platform
{
    public interface INotificationChannel
    {
        Task<OperationResult> SendAsync(
            string brand,
            string account,
            NotificationContext context,
            int template,
            IReadOnlyDictionary<string, string> parameters,
            int level = 0);
    }
}
