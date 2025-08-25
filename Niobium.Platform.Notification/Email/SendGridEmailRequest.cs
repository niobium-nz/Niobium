namespace Niobium.Platform.Notification.Email
{
    public class SendGridEmailRequest
    {
        public required IEnumerable<SendGridEmailBody> Personalizations { get; set; }

        public required SendGridEmailAddress From { get; set; }

        public required string TemplateId { get; set; }

        public required ICollection<SendGridEmailAttachment> Attachments { get; set; }
    }
}
