using Microsoft.Extensions.Logging;

namespace Niobium.Platform.Notification.Email
{
    public class DevelopmentEmailNotificationClient(ILogger<GenericEmailNotificationClient> logger)
        : GenericEmailNotificationClient(logger)
    {
        protected override Task<bool> SendCoreAsync(EmailAddress from, IEnumerable<EmailAddress> recipients, string subject, string body, CancellationToken cancellationToken = default)
        {
#if DEBUG
#pragma warning disable CA1873 // Avoid potentially expensive logging
            Logger.LogInformation($"Sending email [{subject}] from {from} to {string.Join(',', [.. recipients.Select(r => r.Address)])}: {body}");
#pragma warning restore CA1873 // Avoid potentially expensive logging
#endif
            return Task.FromResult(true);
        }
    }
}
