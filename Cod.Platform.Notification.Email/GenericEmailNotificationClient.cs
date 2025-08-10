using Microsoft.Extensions.Logging;

namespace Cod.Platform.Notification.Email
{
    public abstract class GenericEmailNotificationClient(ILogger<GenericEmailNotificationClient> logger) : IEmailNotificationClient
    {
        protected ILogger Logger { get; } = logger;

        public async Task<bool> SendAsync(EmailAddress from, IEnumerable<string> recipients, string subject, string body, CancellationToken cancellationToken = default)
        {
            from.Address = from.Address.Trim().ToLowerInvariant();
            if (!RegexUtilities.IsValidEmail(from.Address))
            {
                logger.LogError($"Email address validation failed: {from.Address}");
                return false;
            }

            IEnumerable<string> tos = recipients.Select(r => r.Trim().ToLowerInvariant());
            foreach (string? recipient in tos)
            {
                if (!RegexUtilities.IsValidEmail(recipient))
                {
                    logger.LogError($"Email address validation failed: {recipient}");
                    return false;
                }
            }

            if (string.IsNullOrWhiteSpace(subject))
            {
                logger.LogError($"Email subject cannot be empty.");
                return false;
            }
            subject = subject.Trim();

            if (string.IsNullOrWhiteSpace(body))
            {
                logger.LogError($"Email body cannot be empty.");
                return false;
            }
            body = body.Trim();

            return await SendCoreAsync(from, tos, subject, body, cancellationToken);
        }

        protected abstract Task<bool> SendCoreAsync(EmailAddress from, IEnumerable<string> recipients, string subject, string body, CancellationToken cancellationToken = default);
    }
}
