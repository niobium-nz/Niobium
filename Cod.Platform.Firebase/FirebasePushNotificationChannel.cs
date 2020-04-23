using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Cod.Platform
{
    public abstract class FirebasePushNotificationChannel : PushNotificationChannel
    {
        private readonly Lazy<ICacheStore> cacheStore;
        private const string AccessTokenCacheKey = "GoogleAccessToken";

        public FirebasePushNotificationChannel(Lazy<IOpenIDManager> openIDManager, Lazy<ICacheStore> cacheStore)
            : base(openIDManager)
        {
            this.cacheStore = cacheStore;
        }

        public async override Task<OperationResult> SendAsync(string brand,
            string account,
            NotificationContext context,
            int template,
            IReadOnlyDictionary<string, object> parameters,
            int level = 0)
        {
            if (level != (int)OpenIDKind.GoogleAndroid
                || (context != null && context.Kind != (int)OpenIDKind.GoogleAndroid))
            {
                return OperationResult.Create(InternalError.NotAllowed);
            }
            return await base.SendAsync(brand, account, context, template, parameters, level);
        }

        protected async override Task<OperationResult> SendPushAsync(
            string brand,
            IEnumerable<NotificationContext> targets,
            int template,
            IReadOnlyDictionary<string, object> parameters)
        {
            var success = true;
            foreach (var target in targets)
            {
                var message = await this.GetMessageAsync(brand, template, target, parameters);
                if (message == null)
                {
                    continue;
                }

                var token = await cacheStore.Value.GetAsync<string>(target.App, AccessTokenCacheKey);
                if (String.IsNullOrWhiteSpace(token))
                {
                    var cred = await this.GetCredentialAsync(target);
                    var scope = cred.CreateScoped("https://www.googleapis.com/auth/firebase.messaging");
                    await scope.UnderlyingCredential.GetAccessTokenForRequestAsync();
                    var t = ((ServiceAccountCredential)scope.UnderlyingCredential).Token;
                    if (t.ExpiresInSeconds.HasValue)
                    {
                        var expiry = DateTimeOffset.UtcNow.AddSeconds(t.ExpiresInSeconds.Value - 1000);
                        await cacheStore.Value.SetAsync(target.App, AccessTokenCacheKey, t.AccessToken, true, expiry);
                    }
                    token = t.AccessToken;
                }

                var request = new FirebaseMessageRequest { Message = message };
                using (var httpclient = new HttpClient(HttpHandler.GetHandler(), false))
                {
                    httpclient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    var json = JsonConvert.SerializeObject(request, JsonSetting.CamelCaseSetting);
                    using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
                    {
                        var resp = await httpclient.PostAsync("https://fcm.googleapis.com/v1/projects/queuesafe/messages:send", content);
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
                }
            }

            if (success)
            {
                return OperationResult.Create();
            }
            else
            {
                return OperationResult.Create((int)HttpStatusCode.InternalServerError);
            }
        }

        protected abstract Task<GoogleCredential> GetCredentialAsync(NotificationContext context);

        protected abstract Task<FirebaseMessage> GetMessageAsync(
            string brand,
            int template,
            NotificationContext context,
            IReadOnlyDictionary<string, object> parameters);
    }
}
