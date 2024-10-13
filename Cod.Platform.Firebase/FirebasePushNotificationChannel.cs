using Cod.Platform.Notification;
using Cod.Platform.Tenant;
using Cod.Table;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;

namespace Cod.Platform
{
    public abstract class FirebasePushNotificationChannel : PushNotificationChannel
    {
        private readonly Lazy<ICacheStore> cacheStore;
        private const string AccessTokenCacheKey = "GooglePushAccessToken";

        public FirebasePushNotificationChannel(Lazy<INofiticationChannelRepository> repo, Lazy<ICacheStore> cacheStore)
            : base(repo) => this.cacheStore = cacheStore;

        public override async Task<OperationResult> SendAsync(string brand,
            Guid user,
            NotificationContext context,
            int template,
            IReadOnlyDictionary<string, object> parameters,
            int level = 0)
        {
            if (level != (int)OpenIDKind.GoogleAndroid
                || (context != null && context.Kind != (int)OpenIDKind.GoogleAndroid))
            {
                return OperationResult.NotAcceptable;
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
                var message = await this.GetMessageAsync(brand, template, target, parameters);
                if (message == null || message.Message == null)
                {
                    continue;
                }

                var token = await this.cacheStore.Value.GetAsync<string>(target.App, AccessTokenCacheKey);
                if (String.IsNullOrWhiteSpace(token))
                {
                    var cred = await this.GetCredentialAsync(target);
                    var scope = cred.CreateScoped("https://www.googleapis.com/auth/firebase.messaging");
                    await scope.UnderlyingCredential.GetAccessTokenForRequestAsync();
                    var t = ((ServiceAccountCredential)scope.UnderlyingCredential).Token;
                    if (t.ExpiresInSeconds.HasValue)
                    {
                        var expiry = DateTimeOffset.UtcNow.AddSeconds(t.ExpiresInSeconds.Value - 1000);
                        await this.cacheStore.Value.SetAsync(target.App, AccessTokenCacheKey, t.AccessToken, true, expiry);
                    }
                    token = t.AccessToken;
                }

                var request = new FirebaseMessageRequest { Message = message.Message };
                using var httpclient = new HttpClient(HttpHandler.GetHandler(), false);
                httpclient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var json = JsonSerializer.SerializeObject(request, JsonSerializationFormat.CamelCase);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                var resp = await httpclient.PostAsync($"https://fcm.googleapis.com/v1/projects/{message.ProjectID}/messages:send", content);
                var status = (int)resp.StatusCode;
                if (status < 200 || status >= 400)
                {
                    success = false;
                    var error = await resp.Content.ReadAsStringAsync();
                    if (Logger.Instance != null)
                    {
                        Logger.Instance.LogError($"An error occurred while making request to Firebase with status code {status}: {error}");
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

        protected abstract Task<GoogleCredential> GetCredentialAsync(NotificationContext context);

        protected abstract Task<ProjectScopeFirebaseMessage> GetMessageAsync(
            string brand,
            int template,
            NotificationContext context,
            IReadOnlyDictionary<string, object> parameters);
    }
}
