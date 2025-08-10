namespace Cod.Channel
{
    public interface INotificationService
    {
        IReadOnlyList<Notification> Notifications { get; }

        Task NotifyAsync(Notification notification);

        void Remove(Guid notificationID);

        void Clear();
    }
}
