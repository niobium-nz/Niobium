namespace Cod.Platform.Notification
{
    public class NotificationContext
    {
        public NotificationContext(
            int kind,
            string app,
            Guid user,
            string identity)
        {
            Kind = kind;
            App = app;
            User = user;
            Identity = identity;
        }

        public int Kind { get; private set; }

        public string App { get; private set; }

        public Guid User { get; private set; }

        public string Identity { get; private set; }
    }
}
