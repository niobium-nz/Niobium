using Microsoft.Extensions.Logging;

namespace Cod.Platform.Notification.Email
{
    public abstract class GenericEmailNotificationClient(ILogger<GenericEmailNotificationClient> logger) : IEmailNotificationClient
    {
        public async Task<bool> SendAsync(string from, IEnumerable<string> recipients, string subject, string body, CancellationToken cancellationToken = default)
        {
            from = from.Trim().ToLowerInvariant();
            if (!RegexUtilities.IsValidEmail(from))
            {
                logger.LogError($"Email address validation failed: {from}");
                return false;
            }

            var tos = recipients.Select(r => r.Trim().ToLowerInvariant());
            foreach (var recipient in tos)
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
            subject = subject.Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(body))
            {
                logger.LogError($"Email body cannot be empty.");
                return false;
            }
            body = body.Trim().ToLowerInvariant();

            return await SendCoreAsync(from, tos, subject, body, cancellationToken);
        }

        protected abstract Task<bool> SendCoreAsync(string from, IEnumerable<string> recipients, string subject, string body, CancellationToken cancellationToken = default);
    }
}
