namespace Cod.Platform.Notification.Email
{
    public interface IEmailNotificationClient
    {
        Task<bool> SendAsync(EmailAddress from, IEnumerable<string> recipients, string subject, string body, CancellationToken cancellationToken = default);
    }
}
