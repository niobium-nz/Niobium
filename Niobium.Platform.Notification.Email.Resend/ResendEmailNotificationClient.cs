using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Niobium.Platform.Notification.Email.Resend
{
    internal sealed class ResendEmailNotificationClient(
        HttpClient httpClient,
        IOptions<ResendServiceOptions> config,
        ILogger<ResendEmailNotificationClient> logger,
        ILogger<GenericEmailNotificationClient> baseLogger) : GenericEmailNotificationClient(baseLogger)
    {
        private const string JSON_CONTENT_TYPE = "application/json";
        private static readonly JsonSerializerOptions SERIALIZATION_OPTIONS = new(JsonSerializerDefaults.Web);

        protected override async Task<bool> SendCoreAsync(EmailAddress from, IEnumerable<string> recipients, string subject, string body, CancellationToken cancellationToken = default)
        {
            string fromDomain = from.Address.Split('@')[1].Trim();
            if (string.IsNullOrWhiteSpace(fromDomain))
            {
                logger.LogError($"From Email address validation failed: {from}");
                return false;
            }

            httpClient.DefaultRequestHeaders.Authorization = config.Value.DomainScopedAPIKeys != null && config.Value.DomainScopedAPIKeys.TryGetValue(fromDomain, out string? value)
                ? new AuthenticationHeaderValue(AuthenticationScheme.BearerLoginScheme, value)
                : new AuthenticationHeaderValue(AuthenticationScheme.BearerLoginScheme, config.Value.GlobalAPIKey);

            string json = Serialize(new ResendRequest
            {
                From = string.IsNullOrWhiteSpace(from.DisplayName) ? from.Address : $"{from.DisplayName.Trim()} <{from.Address}>",
                Html = body,
                Subject = subject,
                To = [.. recipients],
            });

            using HttpResponseMessage response = await httpClient.PostAsync("emails", new StringContent(json, Encoding.UTF8, JSON_CONTENT_TYPE), cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError($"Error getting response from Resend: {response.StatusCode}, {response.ReasonPhrase}.");
                return false;
            }

            string respbody = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogInformation($"Email sent: {respbody}");
            return true;
        }

        private static string Serialize(object obj)
        {
            return System.Text.Json.JsonSerializer.Serialize(obj, SERIALIZATION_OPTIONS);
        }
    }
}
