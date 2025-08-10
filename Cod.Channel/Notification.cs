namespace Cod.Channel
{
    public class Notification(string subject, string body, NotificationLevel level, bool blocked, NotificationHandleOption options, INotificationHandler? handler)
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

        public Guid ID { get; set; } = Guid.NewGuid();

        public string Subject { get; set; } = subject;

        public string Body { get; set; } = body;

        public NotificationLevel Level { get; set; } = level;

        public bool Blocked { get; set; } = blocked;

        public NotificationHandleOption Options { get; set; } = options;

        public INotificationHandler? Handler { get; set; } = handler;
    }
}
