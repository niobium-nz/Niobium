using System;

namespace Cod.Channel
{
    public class Notification
    {
        public Notification(string subject, string body)
            : this(subject, body, NotificationLevel.Information, false, NotificationHandleOption.None, null)
        {
        }

        public Notification(string subject, string body, NotificationLevel level)
            : this(subject, body, level, false, NotificationHandleOption.None, null)
        {
        }

        public Notification(string subject, string body, NotificationLevel level, bool blocked)
            : this(subject, body, level, blocked, NotificationHandleOption.None, null)
        {
        }

        public Notification(string subject, string body, NotificationLevel level, bool blocked, NotificationHandleOption options)
            : this(subject, body, level, blocked, options, null)
        {
        }

        public Notification(string subject, string body, NotificationLevel level, bool blocked, NotificationHandleOption options, INotificationHandler handler)
        {
            this.ID = Guid.NewGuid();
            this.Subject = subject;
            this.Body = body;
            this.Level = level;
            this.Blocked = blocked;
            this.Options = options;
            this.Handler = handler;
        }

        public Guid ID { get; set; }

        public string Subject { get; set; }

        public string Body { get; set; }

        public NotificationLevel Level { get; set; }

        public bool Blocked { get; set; }

        public NotificationHandleOption Options { get; set; }

        public INotificationHandler Handler { get; set; }
    }
}
