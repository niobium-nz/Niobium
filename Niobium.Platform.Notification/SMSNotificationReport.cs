namespace Niobium.Platform.Notification
{
    public class SMSNotificationReport
    {
        public required string Correlation { get; set; }

        public DateTimeOffset Received { get; set; }

        public SMSNotificationStatus Status { get; set; }

        public string? Error { get; set; }
    }
}
