using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Cod.Platform.Captcha.Recaptcha
{
    internal partial class GoogleReCaptchaRiskAssessor(
        HttpClient httpClient,
        IOptions<CaptchaOptions> options,
        ILogger<GoogleReCaptchaRiskAssessor> logger)
        : IVisitorRiskAssessor
    {
        private static readonly JsonSerializerOptions SERIALIZATION_OPTIONS = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };

        public async Task<bool> AssessAsync(Guid requestID, string token, string? clientIP, CancellationToken cancellationToken)
        {
            List<KeyValuePair<string, string>> parameters = new([
                new KeyValuePair<string, string>("secret", options.Value.Secret),
                new KeyValuePair<string, string>("response", token),
            ]);
            if (!string.IsNullOrWhiteSpace(clientIP))
            {
                parameters.Add(new KeyValuePair<string, string>("remoteip", clientIP));
            }
            var payload = new FormUrlEncodedContent(parameters);

            using var response = await httpClient.PostAsync("recaptcha/api/siteverify", payload, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError($"Error response {response.StatusCode} from Google ReCaptcha on request {requestID}.");
                return false;
            }

            var respbody = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = Deserialize<GoogleReCaptchaResult>(respbody);
            if (result == null)
            {
                logger.LogError($"Error deserializing Google ReCaptcha response: {respbody} on request {requestID}.");
                return false;
            }

            return result.Success;
        }

        private static T Deserialize<T>(string json) => System.Text.Json.JsonSerializer.Deserialize<T>(json, SERIALIZATION_OPTIONS)!;
    }
}
