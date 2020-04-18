using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Cod.Platform
{
    public abstract class SendGridEmailNotificationChannel : INotificationChannel
    {
        private static string Key;

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
            string account,
            NotificationContext context,
            int template,
            IReadOnlyDictionary<string, object> parameters, int level = 0)
        {
            if (level != (int)OpenIDKind.Email)
            {
                return OperationResult.Create(InternalError.NotAllowed);
            }

            if (string.IsNullOrWhiteSpace(account))
            {
                return OperationResult.Create(InternalError.NotAllowed);
            }

            if (!RegexUtilities.IsValidEmail(account))
            {
                return OperationResult.Create(InternalError.BadRequest);
            }

            return await SendEmailAsync(brand, account, context, template, parameters);
        }

        protected virtual async Task<OperationResult> SendEmailAsync(
            string brand,
            string account,
            NotificationContext context,
            int template,
            IReadOnlyDictionary<string, object> parameters)
        {
            var requestObj = await this.MakeRequestAsync(brand, account, context, template, parameters);
            var requestData = JsonConvert.SerializeObject(requestObj, JsonSetting.UnderstoreCaseSetting);
            using (var httpclient = new HttpClient(HttpHandler.GetHandler(), false))
            {
                httpclient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Key);
                using (var content = new StringContent(requestData, Encoding.UTF8, "application/json"))
                {
                    var resp = await httpclient.PostAsync("https://api.sendgrid.com/v3/mail/send", content);
                    var status = (int)resp.StatusCode;
                    if (status >= 200 && status < 400)
                    {
                        return OperationResult.Create();
                    }

                    var json = await resp.Content.ReadAsStringAsync();
                    return OperationResult.Create(status, json);
                }
            }
        }

        protected abstract Task<SendGridEmailRequest> MakeRequestAsync(
            string brand,
            string account,
            NotificationContext context,
            int template,
            IReadOnlyDictionary<string, object> parameters);
    }
}
