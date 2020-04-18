namespace Cod.Platform
{
    public class NotificationContext
    {
        public NotificationContext(
            int kind,
            string appID,
            string userID)
        {
            this.Kind = kind;
            this.AppID = appID;
            this.UserID = userID;
        }

        public int Kind { get; private set; }

        public string AppID { get; private set; }

        public string UserID { get; private set; }
    }
}
