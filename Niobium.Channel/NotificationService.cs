namespace Niobium.Channel
{
    internal sealed class NotificationService : INotificationService
    {
        private readonly List<Notification> notifications;

        public IReadOnlyList<Notification> Notifications => notifications;

        public NotificationService()
        {
            notifications = [];
        }

        public Task NotifyAsync(Notification notification)
        {
            if (!notifications.Any(n => n.ID == notification.ID))
            {
                notifications.Add(notification);
            }
            return Task.CompletedTask;
        }

        public void Remove(Guid notificationID)
        {
            notifications.RemoveAll(n => n.ID == notificationID);
        }

        public void Clear()
        {
            notifications.Clear();
        }
    }
}
