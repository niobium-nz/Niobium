using Cod.Table;
using Microsoft.Extensions.Logging;
using System.Collections.Specialized;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Xml;

namespace Cod.Platform.Tenant.Wechat
{
    public class WechatIntegration
    {
        public const string WechatHost = "api.weixin.qq.com";
        private const string WechatPayHost = "api.mch.weixin.qq.com";
        private const string AccessTokenCacheKey = "AccessToken";
        private static readonly TimeSpan AccessTokenCacheExpiry = TimeSpan.FromHours(1);
        private static string wechatProxyHost;
        private static string memoryCachedAccessToken;
        private readonly Lazy<ICacheStore> cacheStore;

        public ILogger Logger { get; }

        public WechatIntegration(Lazy<ICacheStore> cacheStore, ILogger logger)
        {
            this.cacheStore = cacheStore;
            Logger = logger;
        }

        public static void Initialize(string wechatReverseProxy)
        {
            wechatProxyHost = wechatReverseProxy ?? throw new ArgumentNullException(nameof(wechatReverseProxy));
        }

        public async Task<OperationResult<Uri>> GenerateMediaUri(string appId, string secret, string mediaID)
        {
            OperationResult<string> token = await GetAccessTokenAsync(appId, secret);
            return !token.IsSuccess
                ? new OperationResult<Uri>(token)
                : new OperationResult<Uri>(new Uri($"https://{WechatHost}/cgi-bin/media/get?access_token={token.Result}&media_id={mediaID}"));
        }


        public async Task<OperationResult<string>> GetJSApiTicket(string appID, string secret)
        {
            OperationResult<string> token = await GetAccessTokenAsync(appID, secret);
            if (!token.IsSuccess)
            {
                return token;
            }

            using HttpClient httpclient = new(HttpHandler.GetHandler(), false);
            NameValueCollection query = HttpUtility.ParseQueryString(string.Empty);
            query["access_token"] = token.Result;
            query["type"] = "jsapi";
            HttpResponseMessage resp = await httpclient.GetAsync($"https://{WechatHost}/cgi-bin/ticket/getticket?{query}");
            int status = (int)resp.StatusCode;
            string json = await resp.Content.ReadAsStringAsync();
            if (status is >= 200 and < 400)
            {
                JsTicketResult result = JsonSerializer.DeserializeObject<JsTicketResult>(json, JsonSerializationFormat.UnderstoreCase);
                if (!string.IsNullOrWhiteSpace(result.Ticket))
                {
                    return new OperationResult<string>(result.Ticket);
                }

                if (json.Contains("\"errcode\":40001,"))
                {
                    await RevokeAccessTokenAsync(appID);
                    return await GetJSApiTicket(appID, secret);
                }

                Logger.LogError($"An error occurred while trying to get JSAPI ticket for {appID} with status code={status}: {json}");

                return new OperationResult<string>(Cod.InternalError.BadGateway) { Reference = json };
            }
            return new OperationResult<string>(status) { Reference = json };
        }

        public async Task<OperationResult<string>> GetAccessTokenAsync(string appID, string secret)
        {
            if (!string.IsNullOrWhiteSpace(memoryCachedAccessToken))
            {
                return new OperationResult<string>(memoryCachedAccessToken);
            }

            string token = await cacheStore.Value.GetAsync<string>(appID, AccessTokenCacheKey);
            if (!string.IsNullOrWhiteSpace(token))
            {
                return new OperationResult<string>(token);
            }

            using HttpClient httpclient = new(HttpHandler.GetHandler(), false);
            NameValueCollection query = HttpUtility.ParseQueryString(string.Empty);
            query["grant_type"] = "client_credential";
            query["appid"] = appID;
            query["secret"] = secret;
            HttpResponseMessage resp = await httpclient.GetAsync($"{wechatProxyHost}/cgi-bin/token?{query}");
            int status = (int)resp.StatusCode;
            string json = await resp.Content.ReadAsStringAsync();
            if (status is >= 200 and < 400)
            {
                TokenResult result = JsonSerializer.DeserializeObject<TokenResult>(json, JsonSerializationFormat.UnderstoreCase);
                if (!string.IsNullOrWhiteSpace(result.AccessToken))
                {
                    await cacheStore.Value.SetAsync(appID, AccessTokenCacheKey, result.AccessToken, false, DateTimeOffset.UtcNow.Add(AccessTokenCacheExpiry));
                    return new OperationResult<string>(result.AccessToken);
                }
                else
                {
                    Logger.LogError($"An error occurred while trying to aquire Wechat access token for {appID} with status code={status}: {json}");
                    return new OperationResult<string>(Cod.InternalError.BadGateway) { Reference = json };
                }
            }

            Logger.LogError($"An error occurred while trying to aquire Wechat access token for {appID} with status code={status}: {json}");
            return new OperationResult<string>(status) { Reference = json };
        }

        public async Task RevokeAccessTokenAsync(string appID)
        {
            memoryCachedAccessToken = null;
            await cacheStore.Value.DeleteAsync(appID, AccessTokenCacheKey);
        }

        public async Task<OperationResult<string>> GetOpenIDAsync(string appID, string secret, string code)
        {
            using HttpClient httpclient = new(HttpHandler.GetHandler(), false);
            NameValueCollection query = HttpUtility.ParseQueryString(string.Empty);
            query["appid"] = appID;
            query["secret"] = secret;
            query["code"] = code;
            query["grant_type"] = "authorization_code";
            HttpResponseMessage resp = await httpclient.GetAsync($"https://{WechatHost}/sns/oauth2/access_token?{query}");
            int status = (int)resp.StatusCode;
            string json = await resp.Content.ReadAsStringAsync();
            if (status is >= 200 and < 400)
            {
                OpenIdResult result = JsonSerializer.DeserializeObject<OpenIdResult>(json);
                if (!string.IsNullOrWhiteSpace(result.Openid))
                {
                    return new OperationResult<string>(result.Openid);
                }

                if (json.Contains("\"errcode\":40001,"))
                {
                    await RevokeAccessTokenAsync(appID);
                    return await GetOpenIDAsync(appID, secret, code);
                }

                Logger.LogError($"An error occurred while trying to get Wechat open ID for {appID} with status code={status}: {json}");

                return new OperationResult<string>(Cod.InternalError.BadGateway) { Reference = json };
            }
            return new OperationResult<string>(status) { Reference = json };
        }

        public async Task<OperationResult<WechatUserInfo>> GetUserInfoAsync(string appId, string secret, string openID, string lang = "zh_CN")
        {
            OperationResult<string> token = await GetAccessTokenAsync(appId, secret);
            if (!token.IsSuccess)
            {
                return new OperationResult<WechatUserInfo>(token);
            }

            string url = $"https://{WechatHost}/cgi-bin/user/info?access_token={token.Result}&openid={openID}&lang={lang}";
            using HttpClient httpclient = new(HttpHandler.GetHandler(), false);
            HttpResponseMessage resp = await httpclient.GetAsync(url);
            string json = await resp.Content.ReadAsStringAsync();
            int status = (int)resp.StatusCode;
            if (status is >= 200 and < 400)
            {

                if (json.Contains("\"errcode\":40001,"))
                {
                    await RevokeAccessTokenAsync(appId);
                    return await GetUserInfoAsync(appId, secret, openID, lang);
                }

                Logger.LogError($"An error occurred while trying to get user info for {openID} with status code={status}: {json}");

                WechatUserInfo result = JsonSerializer.DeserializeObject<WechatUserInfo>(json);
                return new OperationResult<WechatUserInfo>(result);
            }
            return new OperationResult<WechatUserInfo>(status) { Reference = json };
        }

        public async Task<OperationResult<string>> JSAPIPay(
            string account,
            int amount,
            string appID,
            string device,
            string reference,
            string desc,
            string attach,
            string ip,
            string wechatMerchantID,
            string wechatMerchantNotifyUri,
            string wechatMerchantSignature,
            int retry = 0)
        {
            if (retry > 10)
            {
                return new OperationResult<string>(Cod.InternalError.Conflict);
            }

            if (retry == 1)
            {
                reference += retry.ToString(CultureInfo.InvariantCulture);
            }
            else if (retry > 1)
            {
                reference = $"{reference[0..^1]}{retry}";
            }

            string nonceStr = Guid.NewGuid().ToString("N").ToUpperInvariant();
            Dictionary<string, object> param = new()
            {
                { "appid", appID },
                { "mch_id", wechatMerchantID},
                { "nonce_str" , nonceStr },
                { "device_info" , device },
                { "sign_type", "MD5" },
                { "body", desc },
                { "out_trade_no", reference },
                { "total_fee", amount },
                { "spbill_create_ip", ip },
                { "openid", account },
                { "notify_url", wechatMerchantNotifyUri },
                { "trade_type", "JSAPI" },
                { "attach", attach }
            };
            string sign = MD5Sign(param, wechatMerchantSignature);
            param.Add("sign", sign);

            using HttpClient httpclient = new(HttpHandler.GetHandler(), false);
#if !DEBUG
            httpclient.Timeout = TimeSpan.FromSeconds(5);
#endif
            string xml = GetXML(param);
            Logger.LogInformation($"微信支付调试: attach={attach} device={device} order={reference} xml={xml} retry={retry}");

            HttpResponseMessage resp = await httpclient.PostAsync($"https://{WechatPayHost}/pay/unifiedorder",
                new StringContent(GetXML(param), Encoding.UTF8, "application/xml"));
            int status = (int)resp.StatusCode;
            string body = await resp.Content.ReadAsStringAsync();
            if (status is < 200 or >= 400)
            {
                return new OperationResult<string>(status) { Reference = body };
            }

            Dictionary<string, string> result = FromXML(body);
            if (result["return_code"].ToUpperInvariant() == "SUCCESS" && result["result_code"].ToUpperInvariant() == "SUCCESS")
            {
                return new OperationResult<string>(result["prepay_id"]);
            }
            else if (result.ContainsKey("err_code_des") && result["err_code_des"].Contains("订单号重复"))
            {
                return await JSAPIPay(
                    account, amount, appID, device, reference, desc, attach, ip, wechatMerchantID, wechatMerchantNotifyUri, wechatMerchantSignature, ++retry);
            }

            return new OperationResult<string>(Cod.InternalError.BadGateway) { Reference = body };
        }

        public static Dictionary<string, object> GetJSAPIPaySignature(string prepayID, string appID, string wechatMerchantSignature)
        {
            string nonceStr = Guid.NewGuid().ToString("N").ToUpperInvariant();
            long timeStamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string package = $"prepay_id={prepayID}";
            Dictionary<string, object> resultParam = new()
            {
                    { "appId", appID },
                    { "nonceStr", nonceStr},
                    { "timeStamp", timeStamp.ToString(CultureInfo.InvariantCulture) },
                    { "package", package },
                    { "signType", "MD5" }
                };
            string signature = MD5Sign(resultParam, wechatMerchantSignature);
            resultParam.Add("paySign", signature);
            return resultParam;
        }

        public static Dictionary<string, string> FromXML(string xml)
        {
            XmlDocument xmlDoc = new() { XmlResolver = null };
            xmlDoc.LoadXml(xml);
            XmlNode xmlNode = xmlDoc.FirstChild;
            XmlNodeList nodes = xmlNode.ChildNodes;
            Dictionary<string, string> result = new();
            foreach (XmlNode xn in nodes)
            {
                XmlElement xe = (XmlElement)xn;
                result[xe.Name] = xe.InnerText;
            }
            return result;
        }

        public static string MD5Sign(Dictionary<string, object> param, string signKey)
        {
            IOrderedEnumerable<KeyValuePair<string, string>> sorted = param.Select(kv => new KeyValuePair<string, string>(kv.Key, kv.Value.ToString())).OrderBy(p => p.Key);
            return MD5Sign(sorted, signKey);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA5351:Do Not Use Broken Cryptographic Algorithms", Justification = "StupidWechat")]
        public static string MD5Sign(IOrderedEnumerable<KeyValuePair<string, string>> items, string signKey)
        {
            string tosign = "";
            foreach (KeyValuePair<string, string> item in items)
            {
                tosign += item.Key;
                tosign += "=";
                tosign += item.Value.ToString();
                tosign += "&";
            }
            tosign = $"{tosign}key={signKey}";
            using MD5 md5 = MD5.Create();
            byte[] bs = md5.ComputeHash(Encoding.UTF8.GetBytes(tosign));
            StringBuilder sb = new();
            foreach (byte b in bs)
            {
                sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));
            }
            string sign = sb.ToString().ToUpperInvariant();
            return sign;
        }

        private static string GetXML(Dictionary<string, object> source)
        {
            StringBuilder xml = new();
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
    }
}
