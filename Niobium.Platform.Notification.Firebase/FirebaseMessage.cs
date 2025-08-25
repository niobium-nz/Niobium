namespace Niobium.Platform.Notification.Firebase
{
    public class FirebaseMessage
    {
        public required string Token { get; set; }

        public required object Data { get; set; }

        public required FirebaseNotificationEntry Notification { get; set; }
    }
}
