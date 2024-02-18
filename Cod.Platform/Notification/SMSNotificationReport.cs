namespace Cod.Platform.Notification
{
    public class SMSNotificationReport
    {
        public string Correlation { get; set; }

        public DateTimeOffset Received { get; set; }

        public SMSNotificationStatus Status { get; set; }

        public string Error { get; set; }
    }
}
