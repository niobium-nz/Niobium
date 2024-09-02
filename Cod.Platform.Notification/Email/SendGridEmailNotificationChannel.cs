using Cod.Platform.Tenant;
using System.Net.Http.Headers;
using System.Text;

namespace Cod.Platform.Notification.Email
{
    public abstract class SendGridEmailNotificationChannel : INotificationChannel
    {
        private static string Key;
        private readonly Lazy<INofiticationChannelRepository> openIDManager;

        public SendGridEmailNotificationChannel(Lazy<INofiticationChannelRepository> openIDManager)
        {
            this.openIDManager = openIDManager;
        }

        public static void Initialize(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            Key = key;
        }

        public async Task<OperationResult> SendAsync(
            string brand,
            Guid user,
            NotificationContext context,
            int templateID,
            IReadOnlyDictionary<string, object> parameters, int level = 0)
        {
            if (level != (int)OpenIDKind.Email)
            {
                return OperationResult.NotAllowed;
            }

            string email = null;
            if (parameters.ContainsKey(NotificationParameters.PreferredEmail)
                && parameters[NotificationParameters.PreferredEmail] is string s)
            {
                email = s;
            }

            if (email == null)
            {
                if (user == Guid.Empty)
                {
                    return OperationResult.NotAllowed;
                }

                List<OpenID> channels = await openIDManager.Value.GetChannelsAsync(user, (int)OpenIDKind.Email).ToListAsync();
                if (!channels.Any())
                {
                    return OperationResult.NotAllowed;
                }

                // TODO (5he11) 这里取第一个其实是不正确的
                email = channels.First().Identity;
            }

            return string.IsNullOrWhiteSpace(email)
                ? OperationResult.NotAllowed
                : !RegexUtilities.IsValidEmail(email)
                ? OperationResult.BadRequest
                : await SendEmailAsync(brand, email, context, templateID, parameters);
        }

        protected virtual async Task<OperationResult> SendEmailAsync(
            string brand,
            string email,
            NotificationContext context,
            int templateID,
            IReadOnlyDictionary<string, object> parameters)
        {
            SendGridEmailRequest requestObj = await MakeRequestAsync(brand, email, context, templateID, parameters);
            string requestData = JsonSerializer.SerializeObject(requestObj, JsonSerializationFormat.UnderstoreCase);
            using HttpClient httpclient = new(HttpHandler.GetHandler(), false);
            httpclient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Key);
            using StringContent content = new(requestData, Encoding.UTF8, "application/json");
            HttpResponseMessage resp = await httpclient.PostAsync("https://api.sendgrid.com/v3/mail/send", content);
            int status = (int)resp.StatusCode;
            if (status is >= 200 and < 400)
            {
                return OperationResult.Success;
            }

            string json = await resp.Content.ReadAsStringAsync();
            return new OperationResult(status) { Reference = json };
        }

        protected abstract Task<SendGridEmailRequest> MakeRequestAsync(
            string brand,
            string email,
            NotificationContext context,
            int templateID,
            IReadOnlyDictionary<string, object> parameters);
    }
}
