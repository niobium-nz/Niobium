namespace Niobium.Platform.Notification.Email
{
    public class SendGridEmailAttachment
    {
        public required string Content { get; set; }

        public string? Type { get; set; }

        public required string Filename { get; set; }

        public required string Disposition { get; set; }

        public string? ContentId { get; set; }
    }
}
