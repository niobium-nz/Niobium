using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cod.Platform
{
    public interface INotificationChannel
    {
        Task<OperationResult> SendAsync(
            string brand,
            OpenIDProvider provider,
            string appID,
            string openID,
            int template,
            IReadOnlyDictionary<string, string> parameters,
            int level = 0);
    }
}
