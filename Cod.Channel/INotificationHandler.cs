namespace Cod.Channel
{
    public interface INotificationHandler
    {
        Task HandleAsync(NotificationHandleOption options);
    }
}
