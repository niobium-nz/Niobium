using System.Net.Http.Headers;
using System.Text;

namespace Cod.Platform
{
    public abstract class SendGridEmailNotificationChannel : INotificationChannel
    {
        private static string Key;
        private readonly Lazy<IOpenIDManager> openIDManager;

        public SendGridEmailNotificationChannel(Lazy<IOpenIDManager> openIDManager) => this.openIDManager = openIDManager;

        public static void Initialize(string key)
        {
            if (String.IsNullOrWhiteSpace(key))
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

                var channels = await this.openIDManager.Value.GetChannelsAsync(user, (int)OpenIDKind.Email);
                if (!channels.Any())
                {
                    return OperationResult.NotAllowed;
                }

                // TODO (5he11) 这里取第一个其实是不正确的
                email = channels.First().Identity;
            }

            if (String.IsNullOrWhiteSpace(email))
            {
                return OperationResult.NotAllowed;
            }

            if (!RegexUtilities.IsValidEmail(email))
            {
                return OperationResult.BadRequest;
            }

            return await this.SendEmailAsync(brand, email, context, templateID, parameters);
        }

        protected virtual async Task<OperationResult> SendEmailAsync(
            string brand,
            string email,
            NotificationContext context,
            int templateID,
            IReadOnlyDictionary<string, object> parameters)
        {
            var requestObj = await this.MakeRequestAsync(brand, email, context, templateID, parameters);
            var requestData = JsonSerializer.SerializeObject(requestObj, JsonSerializationFormat.UnderstoreCase);
            using var httpclient = new HttpClient(HttpHandler.GetHandler(), false);
            httpclient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Key);
            using var content = new StringContent(requestData, Encoding.UTF8, "application/json");
            var resp = await httpclient.PostAsync("https://api.sendgrid.com/v3/mail/send", content);
            var status = (int)resp.StatusCode;
            if (status >= 200 && status < 400)
            {
                return OperationResult.Success;
            }

            var json = await resp.Content.ReadAsStringAsync();
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
