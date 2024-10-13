using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cod.Channel
{
    internal class NotificationService : INotificationService
    {
        private readonly List<Notification> notifications;

        public IReadOnlyList<Notification> Notifications => this.notifications;

        public NotificationService() => this.notifications = [];

        public Task NotifyAsync(Notification notification)
        {
            if (!this.notifications.Any(n => n.ID == notification.ID))
            {
                this.notifications.Add(notification);
            }
            return Task.CompletedTask;
        }

        public void Remove(Guid notificationID) => this.notifications.RemoveAll(n => n.ID == notificationID);

        public void Clear() => this.notifications.Clear();
    }
}
