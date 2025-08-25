namespace Niobium.Channel
{
    public interface INotificationHandler
    {
        Task HandleAsync(NotificationHandleOption options);
    }
}
