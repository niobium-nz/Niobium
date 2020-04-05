namespace Cod.Platform
{
    public class NotificationContext
    {
        public NotificationContext(
            OpenIDProvider provider,
            string appID,
            string userID)
        {
            this.Provider = provider;
            this.AppID = appID;
            this.UserID = userID;
        }

        public OpenIDProvider Provider { get; private set; }

        public string AppID { get; private set; }

        public string UserID { get; private set; }
    }
}
