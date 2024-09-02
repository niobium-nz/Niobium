namespace Cod.Platform.Notification.Email
{
    public class SendGridEmailBody
    {
        public IEnumerable<SendGridEmailAddress> To { get; set; }

        public IDictionary<string, object> DynamicTemplateData { get; set; }

        public string Subject { get; set; }
    }
}
