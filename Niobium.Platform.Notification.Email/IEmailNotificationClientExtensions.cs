namespace Niobium.Platform.Notification.Email
{
    public static class IEmailNotificationClientExtensions
    {
        public static Task<bool> SendAsync(this IEmailNotificationClient client, EmailAddress from, IEnumerable<string> recipients, string subject, string body, CancellationToken cancellationToken = default)
        {
            return client.SendAsync(from, recipients.Select(r => new EmailAddress { Address = r }), subject, body, cancellationToken);
        }
    }
}
