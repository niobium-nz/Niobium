using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using Cod.Platform.Model.Wechat;
using Newtonsoft.Json;

namespace Cod.Platform
{
    public static class WechatHelper
    {
        private static string wechatHost;
        private static string wechatPayHost;

        public static void Initialize(string wechatReverseProxy, string wechatPayReverseProxy)
        {
            if (String.IsNullOrWhiteSpace(wechatReverseProxy))
            {
                throw new ArgumentException("message", nameof(wechatReverseProxy));
            }

            if (String.IsNullOrWhiteSpace(wechatPayReverseProxy))
            {
                throw new ArgumentException("message", nameof(wechatPayReverseProxy));
            }

            wechatHost = wechatReverseProxy;
            wechatPayHost = wechatPayReverseProxy;
        }

        public static async Task<OperationResult<string>> GetJSApiTicket(string appID, string secret)
        {
            var token = await GetAccessToken(appID, secret);
            if (!token.IsSuccess)
            {
                return token;
            }

            using (var httpclient = new HttpClient(HttpHandler.GetHandler(), false))
            {
                var query = HttpUtility.ParseQueryString(String.Empty);
                query["access_token"] = token.Result;
                query["type"] = "jsapi";
                var resp = await httpclient.GetAsync($"{wechatHost}/cgi-bin/ticket/getticket?{query.ToString()}");
                var status = (int)resp.StatusCode;
                var json = await resp.Content.ReadAsStringAsync();
                if (status >= 200 && status < 400)
                {
                    var result = JsonConvert.DeserializeObject<JsTicketResult>(json, JsonSetting.UnderstoreCaseSetting);
                    return OperationResult<string>.Create(result.Ticket);
                }
                return OperationResult<string>.Create(status, json);
            }
        }

        private static async Task<OperationResult<string>> GetAccessToken(string appID, string secret)
        {
            using (var httpclient = new HttpClient(HttpHandler.GetHandler(), false))
            {
                var query = HttpUtility.ParseQueryString(String.Empty);
                query["grant_type"] = "client_credential";
                query["appid"] = appID;
                query["secret"] = secret;
                var resp = await httpclient.GetAsync($"{wechatHost}/cgi-bin/token?{query.ToString()}");
                var status = (int)resp.StatusCode;
                var json = await resp.Content.ReadAsStringAsync();
                if (status >= 200 && status < 400)
                {
                    var result = JsonConvert.DeserializeObject<TokenResult>(json, JsonSetting.UnderstoreCaseSetting);
                    return OperationResult<string>.Create(result.AccessToken);
                }
                return OperationResult<string>.Create(status, json);
            }
        }

        public static async Task<OperationResult<string>> GetOpenIDAsync(string appID, string secret, string code)
        {
            using (var httpclient = new HttpClient(HttpHandler.GetHandler(), false))
            {
                var query = HttpUtility.ParseQueryString(String.Empty);
                query["appid"] = appID;
                query["secret"] = secret;
                query["code"] = code;
                query["grant_type"] = "authorization_code";
                var resp = await httpclient.GetAsync($"{wechatHost}/sns/oauth2/access_token?{query.ToString()}");
                var status = (int)resp.StatusCode;
                var json = await resp.Content.ReadAsStringAsync();
                if (status >= 200 && status < 400)
                {
                    var result = JsonConvert.DeserializeObject<OpenIdResult>(json);
                    return OperationResult<string>.Create(result.Openid);
                }
                return OperationResult<string>.Create(status, json);
            }
        }

        public static async Task<OperationResult<string>> JSAPIPay(string account, int amount, string appID, string device, string order, string product, string attach, string ip,
                    string wechatMerchantID, string wechatMerchantNotifyUri, string wechatMerchantSignature)
        {
            var nonceStr = Guid.NewGuid().ToString("N").ToUpperInvariant();
            var param = new Dictionary<string, object>
            {
                { "appid", appID },
                { "mch_id", wechatMerchantID},
                { "nonce_str" , nonceStr },
                { "device_info" , device },
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
            var sign = param.MD5Sign(wechatMerchantSignature);
            param.Add("sign", sign);

            using (var httpclient = new HttpClient(HttpHandler.GetHandler(), false))
            {
                var resp = await httpclient.PostAsync($"{wechatPayHost}/pay/unifiedorder",
                    new StringContent(GetXML(param), Encoding.UTF8, "application/xml"));
                var status = (int)resp.StatusCode;
                var body = await resp.Content.ReadAsStringAsync();
                if (status < 200 || status >= 400)
                {
                    return OperationResult<string>.Create(status, body);
                }

                var result = FromXML(body);
                if (result["return_code"].ToUpperInvariant() != "SUCCESS")
                {
                    return OperationResult<string>.Create(InternalError.Unknown, body);
                }
                return OperationResult<string>.Create(result["prepay_id"]);
            }
        }

        public static Dictionary<string, object> GetJSAPIPaySignature(string prepayID, string appID, string wechatMerchantSignature)
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
            var signature = resultParam.MD5Sign(wechatMerchantSignature);
            resultParam.Add("paySign", signature);
            return resultParam;
        }

        private static string GetXML(Dictionary<string, object> source)
        {
            var xml = new StringBuilder();
            xml.Append("<xml>");
            foreach (var pair in source)
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
            var xmlDoc = new XmlDocument() { XmlResolver = null };
            xmlDoc.LoadXml(xml);
            var xmlNode = xmlDoc.FirstChild;
            var nodes = xmlNode.ChildNodes;
            var result = new Dictionary<string, string>();
            foreach (XmlNode xn in nodes)
            {
                var xe = (XmlElement)xn;
                result[xe.Name] = xe.InnerText;
            }
            return result;
        }

        public static string MD5Sign(this Dictionary<string, object> param, string signKey)
        {
            var sorted = param.Select(kv => new KeyValuePair<string, string>(kv.Key, kv.Value.ToString())).OrderBy(p => p.Key);
            return sorted.MD5Sign(signKey);
        }

        public static string MD5Sign(this IOrderedEnumerable<KeyValuePair<string, string>> items, string signKey)
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
                foreach (var b in bs)
                {
                    sb.Append(b.ToString("x2"));
                }
                var sign = sb.ToString().ToUpper();
                return sign;
            }
        }
    }
}
