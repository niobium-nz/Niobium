using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;

namespace Niobium.Platform.Notification.Firebase
{
    public abstract class FirebasePushNotificationChannel(Lazy<INofiticationChannelRepository> repo, Lazy<ICacheStore> cacheStore) : PushNotificationChannel(repo)
    {
        private const string AccessTokenCacheKey = "GooglePushAccessToken";

        public override async Task<OperationResult> SendAsync(string brand,
            Guid user,
            NotificationContext context,
            int templateID,
            IReadOnlyDictionary<string, object> parameters,
            int level = 0)
        {
            return level != (int)OpenIDKind.GoogleAndroid
                || (context != null && context.Kind != (int)OpenIDKind.GoogleAndroid)
                ? OperationResult.NotAcceptable
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
                ProjectScopeFirebaseMessage message = await GetMessageAsync(brand, templateID, target, parameters);
                if (message == null || message.Message == null)
                {
                    continue;
                }

                string? token = await cacheStore.Value.GetAsync<string>(target.App, AccessTokenCacheKey);
                if (string.IsNullOrWhiteSpace(token))
                {
                    GoogleCredential cred = await GetCredentialAsync(target);
                    GoogleCredential scope = cred.CreateScoped("https://www.googleapis.com/auth/firebase.messaging");
                    await scope.UnderlyingCredential.GetAccessTokenForRequestAsync();
                    Google.Apis.Auth.OAuth2.Responses.TokenResponse t = ((ServiceAccountCredential)scope.UnderlyingCredential).Token;
                    if (t.ExpiresInSeconds.HasValue)
                    {
                        DateTimeOffset expiry = DateTimeOffset.UtcNow.AddSeconds(t.ExpiresInSeconds.Value - 1000);
                        await cacheStore.Value.SetAsync(target.App, AccessTokenCacheKey, t.AccessToken, true, expiry);
                    }
                    token = t.AccessToken;
                }

                FirebaseMessageRequest request = new() { Message = message.Message };
                using HttpClient httpclient = new(HttpHandler.GetHandler(), false);
                httpclient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(AuthenticationScheme.BearerLoginScheme, token);
                string json = JsonMarshaller.Marshall(request, JsonMarshallingFormat.CamelCase);
                using StringContent content = new(json, Encoding.UTF8, "application/json");
                HttpResponseMessage resp = await httpclient.PostAsync($"https://fcm.googleapis.com/v1/projects/{message.ProjectID}/messages:send", content);
                int status = (int)resp.StatusCode;
                if (status is < 200 or >= 400)
                {
                    success = false;
                    string error = await resp.Content.ReadAsStringAsync();
                    Logger.Instance?.LogError($"An error occurred while making request to Firebase with status code {status}: {error}");
                }
            }

            return success ? OperationResult.Success : OperationResult.InternalServerError;
        }

        protected abstract Task<GoogleCredential> GetCredentialAsync(NotificationContext context);

        protected abstract Task<ProjectScopeFirebaseMessage> GetMessageAsync(
            string brand,
            int templateID,
            NotificationContext context,
            IReadOnlyDictionary<string, object> parameters);
    }
}
