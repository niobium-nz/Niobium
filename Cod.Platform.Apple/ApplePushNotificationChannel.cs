using Cod.Platform.Notification;
using Cod.Platform.Tenant;
using Cod.Table;
using Jose;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

namespace Cod.Platform
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "WindowsOnly")]
    public abstract class ApplePushNotificationChannel : PushNotificationChannel
    {
        private readonly Lazy<ICacheStore> cacheStore;
        private const string AccessTokenCacheKey = "ApplePushAccessToken";

        public ApplePushNotificationChannel(Lazy<INofiticationChannelRepository> repo, Lazy<ICacheStore> cacheStore)
            : base(repo) => this.cacheStore = cacheStore;

        protected virtual string ApplePushNotificationHost => "api.sandbox.push.apple.com";

        public override async Task<OperationResult> SendAsync(string brand,
            Guid user,
            NotificationContext context,
            int template,
            IReadOnlyDictionary<string, object> parameters,
            int level = 0)
        {
            if (level != (int)OpenIDKind.iOS
                || (context != null && context.Kind != (int)OpenIDKind.iOS))
            {
                return new OperationResult(InternalError.NotAcceptable);
            }
            return await base.SendAsync(brand, user, context, template, parameters, level);
        }

        protected override async Task<OperationResult> SendPushAsync(
            string brand,
            IEnumerable<NotificationContext> targets,
            int template,
            IReadOnlyDictionary<string, object> parameters)
        {
            var success = true;
            foreach (var target in targets)
            {
                var messages = await this.GetMessagesAsync(brand, template, target, parameters);
                if (messages == null || !messages.Any())
                {
                    continue;
                }

                var token = await this.cacheStore.Value.GetAsync<string>(target.App, AccessTokenCacheKey);
                if (String.IsNullOrWhiteSpace(token))
                {
                    token = await this.IssueTokenAsync(target);
                    await this.cacheStore.Value.SetAsync(target.App, AccessTokenCacheKey, token, false, DateTimeOffset.UtcNow.AddMinutes(30));
                }

                using var httpclient = new HttpClient(HttpHandler.GetHandler(), false);
                foreach (var message in messages)
                {
                    using var request = new HttpRequestMessage(HttpMethod.Post, $"https://{this.ApplePushNotificationHost}/3/device/{target.Identity}")
                    {
                        Version = new Version(2, 0)
                    };
                    request.Headers.Add("apns-push-type", message.Background ? "background" : "alert");
                    request.Headers.Add("apns-id", message.ID.ToString());
                    request.Headers.Add("apns-expiration", message.Expires.ToUnixTimeSeconds().ToString());
                    request.Headers.Add("apns-topic", message.Topic);
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                    var sb = new StringBuilder();
                    if (message.Background)
                    {
                        sb.Append("{\"aps\":{\"content-available\":1},");
                        var json = JsonSerializer.SerializeObject(message.Message, JsonSerializationFormat.CamelCase);
                        sb.Append(json[1..]);
                    }
                    else
                    {
                        sb.Append("{\"aps\":{\"alert\":\"");
                        sb.Append((string)message.Message);
                        sb.Append("\"}}");
                    }

                    using var content = new StringContent(sb.ToString(), Encoding.UTF8, "application/json");
                    request.Content = content;
                    using var resp = await httpclient.SendAsync(request);
                    var status = (int)resp.StatusCode;
                    if (status < 200 || status >= 400)
                    {
                        success = false;
                        var error = await resp.Content.ReadAsStringAsync();
                        if (Logger.Instance != null)
                        {
                            Logger.Instance.LogError($"An error occurred while making request to APN with status code {status}: {error}");
                        }
                    }
                }
            }

            if (success)
            {
                return OperationResult.Success;
            }
            else
            {
                return OperationResult.InternalServerError;
            }
        }

        protected abstract Task<ApplePushCredential> GetCredentialAsync(NotificationContext context);

        protected abstract Task<IEnumerable<ApplePushNotification>> GetMessagesAsync(
            string brand,
            int template,
            NotificationContext context,
            IReadOnlyDictionary<string, object> parameters);

        private async Task<string> IssueTokenAsync(NotificationContext context)
        {
            var cred = await this.GetCredentialAsync(context);
            var iat = DateTimeOffset.UtcNow.AddMinutes(-1).ToUnixTimeSeconds();
            var exp = DateTimeOffset.UtcNow.AddMinutes(30).ToUnixTimeSeconds();
            var payload = new Dictionary<string, object>()
                {
                    { "iat", iat },
                    { "exp", exp },
                    { "iss", cred.TeamID },
                };
            var extraHeader = new Dictionary<string, object>()
                {
                    { "alg", "ES256" },
                    { "typ", "JWT" },
                    { "kid", cred.KeyID },
                };
            var privateKey = CngKey.Import(Convert.FromBase64String(cred.Key), CngKeyBlobFormat.Pkcs8PrivateBlob);
            return JWT.Encode(payload, privateKey, JwsAlgorithm.ES256, extraHeader);
        }
    }
}
