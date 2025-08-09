namespace Cod.Platform.Notification.Apple
{
    public class ApplePushNotification
    {
        public bool Background { get; set; }

        public Guid ID { get; set; }

        public DateTimeOffset Expires { get; set; }

        public required string Topic { get; set; }

        public required object Message { get; set; }
    }
}
