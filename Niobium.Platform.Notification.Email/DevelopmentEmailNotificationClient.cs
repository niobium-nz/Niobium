using Microsoft.Extensions.Logging;

namespace Niobium.Platform.Notification.Email
{
    public class DevelopmentEmailNotificationClient(ILogger<GenericEmailNotificationClient> logger)
        : GenericEmailNotificationClient(logger)
    {
        protected override Task<bool> SendCoreAsync(EmailAddress from, IEnumerable<EmailAddress> recipients, string subject, string body, CancellationToken cancellationToken = default)
        {
            Logger.LogInformation($"Sending email [{subject}] from {from} to {string.Join(',', [.. recipients.Select(r => r.Address)])}: {body}");
            return Task.FromResult(true);
        }
    }
}
