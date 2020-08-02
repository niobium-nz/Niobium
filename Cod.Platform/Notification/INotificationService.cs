using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cod.Platform
{
    public interface INotificationService
    {
        Task<OperationResult<int>> SendAsync(
            string brand,
            Guid user,
            NotificationContext context,
            int template,
            IReadOnlyDictionary<string, object> parameters,
            int startLevel = 0,
            int maxLevel = 100);
    }
}
