using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Cod.Platform.Charges;
using Cod.Platform.Model.Wechat;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Pathoschild.Http.Client;

namespace Cod.Platform
{
    internal static class WechatHelper
    {
        public static async Task<string> GetJSApiTicket(string appID, string secret)
        {
            var token = await GetAccessToken(appID, secret);
            using (var httpclient = new HttpClient(HttpHandler.GetHandler(), false))
            using (var client = new FluentClient("http://api.weixin.niaoju.net/cgi-bin", httpclient).SetOptions(true, true))
            {
                var response = await client.GetAsync("ticket/getticket")
                    .WithArgument("access_token", token)
                    .WithArgument("type", "jsapi")
                    .AsString();
                var result = JsonConvert.DeserializeObject<JsTicketResult>(response, JsonSetting.UnderstoreCaseSetting);
                return result.Ticket;
            }
        }

        private static async Task<string> GetAccessToken(string appID, string secret)
        {
            using (var httpclient = new HttpClient(HttpHandler.GetHandler(), false))
            using (var client = new FluentClient("http://api.weixin.niaoju.net/cgi-bin", httpclient).SetOptions(true, true))
            {
                var response = await client.GetAsync("token")
                    .WithArgument("grant_type", "client_credential")
                    .WithArgument("appid", appID)
                    .WithArgument("secret", secret)
                    .AsString();
                var result = JsonConvert.DeserializeObject<TokenResult>(response, JsonSetting.UnderstoreCaseSetting);
                return result.AccessToken;
            }
        }

        internal static bool ValidateNotification(WechatChargeNotification notify, string signatureKey,string chargeSecret, ILogger logger)
        {
            if (notify.ResultCode.ToUpperInvariant() != "SUCCESS")
            {
                logger.LogInformation($"微信回调通知失败 错误消息:{notify.ResultMessage}");
                return false;
            }

            var signature = notify.WechatDatas.Where(k => k.Key != "sign").OrderBy(k => k.Key).MD5Sign(signatureKey, logger);
            if (signature != notify.WechatSignature)
            {
                logger.LogInformation($"验证微信签名失败. 微信返回签名:{notify.WechatSignature} 自签:{signature}");
                return false;
            }
            var toSign = $"{notify.AppID}|{notify.Account}|{notify.Amount}";
            var internalSignature = SHA.SHA256Hash(toSign, chargeSecret, 127);
            if (internalSignature != notify.InternalSignature)
            {
                logger.LogInformation($"验证内部签名失败. 微信返回内部签名:{notify.InternalSignature} 自签:{internalSignature}");
                return false;
            }
            return true;
        }

        public static async Task<string> GetOpenIDAsync(string appID, string secret, string code)
        {
            using (var httpclient = new HttpClient(HttpHandler.GetHandler(), false))
            using (var client = new FluentClient("http://api.weixin.niaoju.net/sns/oauth2", httpclient).SetOptions(true, true))
            {
                var response = await client.GetAsync("access_token")
                    .WithArgument("appid", appID)
                    .WithArgument("secret", secret)
                    .WithArgument("code", code)
                    .WithArgument("grant_type", "authorization_code")
                    .AsString();
                var result = JsonConvert.DeserializeObject<OpenIdResult>(response);
                return result.Openid;
            }
        }

        public static async Task<string> JSAPIPay(string account, int amount, string appID, string order, string product, string attach, string ip,
                    string wechatMerchantID, string wechatMerchantNotifyUri, string wechatMerchantSignature, ILogger logger)
        {
            var nonceStr = Guid.NewGuid().ToString("N").ToUpperInvariant();
            var param = new Dictionary<string, object>
            {
                { "appid", appID },
                { "mch_id", wechatMerchantID},
                { "nonce_str" , nonceStr },
                { "sign_type", "MD5" },
                { "body", product },
                { "out_trade_no", order },
                { "total_fee", amount },
                { "spbill_create_ip", ip },
                { "openid", account },
                { "notify_url", wechatMerchantNotifyUri },
                { "trade_type", "JSAPI" },
                { "attach", attach }
            };
            var sign = param.MD5Sign(wechatMerchantSignature, logger);
            param.Add("sign", sign);

            using (var httpclient = new HttpClient(HttpHandler.GetHandler(), false))
            using (var client = new FluentClient("http://api.mch.weixin.niaoju.net", httpclient).SetOptions(true, true))
            {
                var response = await client.PostAsync("pay/unifiedorder").WithBody(GetXML(param)).AsString();
                logger.LogInformation($"调用统一支付接口返回结果:{response}");
                var result = FromXML(response);
                if (result["return_code"].ToUpperInvariant() != "SUCCESS")
                {
                    //TODO 错误消息
                    logger.LogError($"支付失败:{response}");
                    return null;
                }
                return result["prepay_id"];
            }
        }

        public static Dictionary<string, object> GetJSAPIPaySignature(string prepayID, string appID, string wechatMerchantSignature, ILogger logger)
        {
            var nonceStr = Guid.NewGuid().ToString("N").ToUpperInvariant();
            var timeStamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var package = $"prepay_id={prepayID}";
            var resultParam = new Dictionary<string, object>
                {
                    { "appId", appID },
                    { "nonceStr", nonceStr},
                    { "timeStamp", timeStamp.ToString() },
                    { "package", package },
                    { "signType", "MD5" }
                };
            var signature = resultParam.MD5Sign(wechatMerchantSignature, logger);
            resultParam.Add("paySign", signature);
            return resultParam;
        }

        private static string GetXML(Dictionary<string, object> source)
        {
            var xml = new StringBuilder();
            xml.Append("<xml>");
            foreach (KeyValuePair<string, object> pair in source)
            {
                //字段值不能为null，会影响后续流程
                if (pair.Value.GetType() == typeof(int))
                {
                    xml.Append("<" + pair.Key + ">" + pair.Value + "</" + pair.Key + ">");
                }
                else if (pair.Value.GetType() == typeof(string))
                {
                    xml.Append("<" + pair.Key + ">" + "<![CDATA[" + pair.Value + "]]></" + pair.Key + ">");
                }
            }
            xml.Append("</xml>");
            return xml.ToString();
        }

        public static Dictionary<string, string> FromXML(string xml)
        {
            XmlDocument xmlDoc = new XmlDocument() { XmlResolver = null };
            xmlDoc.LoadXml(xml);
            XmlNode xmlNode = xmlDoc.FirstChild;
            XmlNodeList nodes = xmlNode.ChildNodes;
            var result = new Dictionary<string, string>();
            foreach (XmlNode xn in nodes)
            {
                XmlElement xe = (XmlElement)xn;
                result[xe.Name] = xe.InnerText;
            }
            return result;
        }

        public static string MD5Sign(this Dictionary<string, object> param, string signKey, ILogger log)
        {
            var sorted = param.Select(kv => new KeyValuePair<string, string>(kv.Key, kv.Value.ToString())).OrderBy(p => p.Key);
            return sorted.MD5Sign(signKey, log);
        }

        public static string MD5Sign(this IOrderedEnumerable<KeyValuePair<string, string>> items, string signKey, ILogger logger)
        {
            var tosign = "";
            foreach (var item in items)
            {
                tosign += item.Key;
                tosign += "=";
                tosign += item.Value.ToString();
                tosign += "&";
            }
            tosign = $"{tosign}key={signKey}";
            using (var md5 = MD5.Create())
            {
                var bs = md5.ComputeHash(Encoding.UTF8.GetBytes(tosign));
                var sb = new StringBuilder();
                foreach (byte b in bs)
                {
                    sb.Append(b.ToString("x2"));
                }
                //所有字符转为大写
                var sign = sb.ToString().ToUpper();
                logger.LogInformation($"待签名数据:{tosign},签名结果:{sign}");
                return sign;
            }
        }
    }
}
