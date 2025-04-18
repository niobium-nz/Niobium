using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Cod.Platform.Notification.Email.Resend
{
    internal class ResendEmailNotificationClient(
        HttpClient httpClient,
        IOptions<ResendServiceOptions> config,
        ILogger<ResendEmailNotificationClient> logger,
        ILogger<GenericEmailNotificationClient> baseLogger) : GenericEmailNotificationClient(baseLogger)
    {
        private const string JSON_CONTENT_TYPE = "application/json";
        private static readonly JsonSerializerOptions SERIALIZATION_OPTIONS = new(JsonSerializerDefaults.Web);

        protected async override Task<bool> SendCoreAsync(string from, IEnumerable<string> recipients, string subject, string body, CancellationToken cancellationToken = default)
        {
            var fromDomain = from.Split('@')[1].Trim();
            if (string.IsNullOrWhiteSpace(fromDomain))
            {
                logger.LogError($"From Email address validation failed: {from}");
                return false;
            }

            if (config.Value.DomainScopedAPIKeys.TryGetValue(fromDomain, out string? value))
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", value);
            }
            else
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", config.Value.GlobalAPIKey);
            }

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
