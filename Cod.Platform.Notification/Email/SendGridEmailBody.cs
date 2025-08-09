namespace Cod.Platform.Notification.Email
{
    public class SendGridEmailBody
    {
        public required IEnumerable<SendGridEmailAddress> To { get; set; }

        public required IDictionary<string, object> DynamicTemplateData { get; set; }

        public required string Subject { get; set; }
    }
}
