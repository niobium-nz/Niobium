namespace Niobium.Platform.Notification
{
    public class NotificationContext(
        int kind,
        string app,
        Guid user,
        string identity)
    {
        public int Kind { get; private set; } = kind;

        public string App { get; private set; } = app;

        public Guid User { get; private set; } = user;

        public string Identity { get; private set; } = identity;
    }
}
