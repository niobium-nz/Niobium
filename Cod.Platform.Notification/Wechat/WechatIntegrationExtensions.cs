using Cod.Platform.Tenant.Wechat;
using Microsoft.Extensions.Logging;
using System.Web;

namespace Cod.Platform.Notification.Wechat
{
    public static class WechatIntegrationExtensions
    {
        public static async Task<OperationResult<string>> SendNotificationAsync(this WechatIntegration integration, string appId, string secret, string openId, string templateId, WechatNotificationParameter parameters, string link)
        {
            OperationResult<string> token = await integration.GetAccessTokenAsync(appId, secret);
            if (!token.IsSuccess)
            {
                return token;
            }

            using HttpClient httpclient = new(HttpHandler.GetHandler(), false);
#if !DEBUG
                httpclient.Timeout = TimeSpan.FromSeconds(10);
#endif
            System.Collections.Specialized.NameValueCollection query = HttpUtility.ParseQueryString(string.Empty);
            query["access_token"] = token.Result;
            WechatTemplateMessageRequest request = new()
            {
                Data = "JSON_DATA",
                TemplateId = templateId,
                Touser = openId,
                Url = link
            };
            string data = JsonSerializer.SerializeObject(request, JsonSerializationFormat.UnderstoreCase);
            data = data.Replace("\"JSON_DATA\"", parameters.ToJson());
            using StringContent content = new(data);
            HttpResponseMessage resp = await httpclient.PostAsync($"https://{WechatIntegration.WechatHost}/cgi-bin/message/template/send?{query}", content);

            int status = (int)resp.StatusCode;
            string json = await resp.Content.ReadAsStringAsync();
            if (status is >= 200 and < 400)
            {
                WechatTemplateMessageResponse result = JsonSerializer.DeserializeObject<WechatTemplateMessageResponse>(json);
                if (result.ErrCode != 0)
                {
                    if (result.ErrCode == 40001)
                    {
                        await integration.RevokeAccessTokenAsync(appId);
                        return await integration.SendNotificationAsync(appId, secret, openId, templateId, parameters, link);
                    }

                    integration.Logger.LogError($"An error occurred while trying to send Wechat notification to {openId} on {appId} with status code={status}: {json}");

                    return new OperationResult<string>(Cod.InternalError.BadGateway) { Reference = json };
                }
                else
                {
                    return new OperationResult<string>(json);
                }
            }

            integration.Logger.LogError($"An error occurred while trying to send Wechat notification to {openId} on {appId} with status code={status}: {json}");

            return new OperationResult<string>(status) { Reference = json };
        }
    }
}
