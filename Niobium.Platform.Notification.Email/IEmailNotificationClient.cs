namespace Niobium.Platform.Notification.Email
{
    public interface IEmailNotificationClient
    {
        Task<bool> SendAsync(EmailAddress from, IEnumerable<EmailAddress> recipients, string subject, string body, CancellationToken cancellationToken = default);
    }
}
