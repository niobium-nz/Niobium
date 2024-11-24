using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace Cod.Platform.Notification.Email.Resend
{
    internal class ResendEmailNotificationClient(
        HttpClient httpClient,
        ILogger<ResendEmailNotificationClient> logger,
        ILogger<GenericEmailNotificationClient> baseLogger) : GenericEmailNotificationClient(baseLogger)
    {
        private const string JSON_CONTENT_TYPE = "application/json";
        private static readonly JsonSerializerOptions SERIALIZATION_OPTIONS = new(JsonSerializerDefaults.Web);

        protected async override Task<bool> SendCoreAsync(string from, IEnumerable<string> recipients, string subject, string body, CancellationToken cancellationToken = default)
        {
            var json = Serialize(new ResendRequest
            {
                From = from,
                Html = body,
                Subject = subject,
                To = recipients.ToArray(),
            });

            using var response = await httpClient.PostAsync("emails", new StringContent(json, Encoding.UTF8, JSON_CONTENT_TYPE), cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError($"Error getting response from Resend: {response.StatusCode}, {response.ReasonPhrase}.");
                return false;
            }

            var respbody = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogInformation($"Email sent: {respbody}");
            return true;
        }

        private static string Serialize(object obj) => System.Text.Json.JsonSerializer.Serialize(obj, SERIALIZATION_OPTIONS);
    }
}
}
