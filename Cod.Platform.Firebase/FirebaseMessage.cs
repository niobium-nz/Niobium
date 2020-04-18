namespace Cod.Platform
{
    public class FirebaseMessage
    {
        public string Token { get; set; }

        public object Data { get; set; }

        public FirebaseNotificationEntry Notification { get; set; }
    }
}
