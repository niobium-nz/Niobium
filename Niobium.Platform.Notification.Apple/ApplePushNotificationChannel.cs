using Jose;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

namespace Niobium.Platform.Notification.Apple
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "WindowsOnly")]
    public abstract class ApplePushNotificationChannel(Lazy<INofiticationChannelRepository> repo, Lazy<ICacheStore> cacheStore) : PushNotificationChannel(repo)
    {
        private const string AccessTokenCacheKey = "ApplePushAccessToken";

        protected virtual string ApplePushNotificationHost => "api.sandbox.push.apple.com";

        public override async Task<OperationResult> SendAsync(string brand, Guid user, NotificationContext context, int templateID, IReadOnlyDictionary<string, object> parameters, int level = 0)
        {
            return level != (int)OpenIDKind.iOS
                || (context != null && context.Kind != (int)OpenIDKind.iOS)
                ? new OperationResult(Niobium.InternalError.NotAcceptable)
                : await base.SendAsync(brand, user, context!, templateID, parameters, level);
        }

        protected override async Task<OperationResult> SendPushAsync(
            string brand,
            IEnumerable<NotificationContext> targets,
            int templateID,
            IReadOnlyDictionary<string, object> parameters)
        {
            bool success = true;
            foreach (NotificationContext target in targets)
            {
                IEnumerable<ApplePushNotification> messages = await GetMessagesAsync(brand, templateID, target, parameters);
                if (messages == null || !messages.Any())
                {
                    continue;
                }

                string? token = await cacheStore.Value.GetAsync<string>(target.App, AccessTokenCacheKey);
                if (string.IsNullOrWhiteSpace(token))
                {
                    token = await IssueTokenAsync(target);
                    await cacheStore.Value.SetAsync(target.App, AccessTokenCacheKey, token, false, DateTimeOffset.UtcNow.AddMinutes(30));
                }

                using HttpClient httpclient = new(HttpHandler.GetHandler(), false);
                foreach (ApplePushNotification message in messages)
                {
                    using HttpRequestMessage request = new(HttpMethod.Post, $"https://{ApplePushNotificationHost}/3/device/{target.Identity}")
                    {
                        Version = new Version(2, 0)
                    };
                    request.Headers.Add("apns-push-type", message.Background ? "background" : "alert");
                    request.Headers.Add("apns-id", message.ID.ToString());
                    request.Headers.Add("apns-expiration", message.Expires.ToUnixTimeSeconds().ToString());
                    request.Headers.Add("apns-topic", message.Topic);
                    request.Headers.Authorization = new AuthenticationHeaderValue(AuthenticationScheme.BearerLoginScheme, token);

                    StringBuilder sb = new();
                    if (message.Background)
                    {
                        sb.Append("{\"aps\":{\"content-available\":1},");
                        string json = JsonSerializer.SerializeObject(message.Message, JsonSerializationFormat.CamelCase);
                        sb.Append(json[1..]);
                    }
                    else
                    {
                        sb.Append("{\"aps\":{\"alert\":\"");
                        sb.Append((string)message.Message);
                        sb.Append("\"}}");
                    }

                    using StringContent content = new(sb.ToString(), Encoding.UTF8, "application/json");
                    request.Content = content;
                    using HttpResponseMessage resp = await httpclient.SendAsync(request);
                    int status = (int)resp.StatusCode;
                    if (status is < 200 or >= 400)
                    {
                        success = false;
                        string error = await resp.Content.ReadAsStringAsync();
                        Logger.Instance?.LogError($"An error occurred while making request to APN with status code {status}: {error}");
                    }
                }
            }

            return success ? OperationResult.Success : OperationResult.InternalServerError;
        }

        protected abstract Task<ApplePushCredential> GetCredentialAsync(NotificationContext context);

        protected abstract Task<IEnumerable<ApplePushNotification>> GetMessagesAsync(
            string brand,
            int templateID,
            NotificationContext context,
            IReadOnlyDictionary<string, object> parameters);

        private async Task<string> IssueTokenAsync(NotificationContext context)
        {
            ApplePushCredential cred = await GetCredentialAsync(context);
            long iat = DateTimeOffset.UtcNow.AddMinutes(-1).ToUnixTimeSeconds();
            long exp = DateTimeOffset.UtcNow.AddMinutes(30).ToUnixTimeSeconds();
            Dictionary<string, object> payload = new()
                {
                    { "iat", iat },
                    { "exp", exp },
                    { "iss", cred.TeamID },
                };
            Dictionary<string, object> extraHeader = new()
                {
                    { "alg", "ES256" },
                    { "typ", "JWT" },
                    { "kid", cred.KeyID },
                };
            CngKey privateKey = CngKey.Import(Convert.FromBase64String(cred.Key), CngKeyBlobFormat.Pkcs8PrivateBlob);
            return JWT.Encode(payload, privateKey, JwsAlgorithm.ES256, extraHeader);
        }
    }
}
