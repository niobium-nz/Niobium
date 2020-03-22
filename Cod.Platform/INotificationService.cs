using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cod.Platform
{
    public interface INotificationService
    {
        Task<OperationResult> SendAsync(
            OpenIDProvider provider,
            string appID,
            string openID,
            int template,
            IReadOnlyDictionary<string, string> parameters,
            int startLevel = 0,
            int maxLevel = 10);
    }
}
