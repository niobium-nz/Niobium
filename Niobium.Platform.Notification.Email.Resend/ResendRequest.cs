namespace Niobium.Platform.Notification.Email.Resend
{
    internal sealed class ResendRequest
    {
        public required string From { get; set; }

        public required string[] To { get; set; }

        public required string Subject { get; set; }

        public required string Html { get; set; }
    }
}
