using System.Threading.Tasks;

namespace Cod.Channel
{
    public interface INotificationHandler
    {
        Task HandleAsync(NotificationHandleOption option);
    }
}
