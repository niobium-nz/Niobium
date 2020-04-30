using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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
    public class WechatIntegration
    {
        private const string WechatHost = "api.weixin.qq.com";
        private const string WechatPayHost = "api.mch.weixin.qq.com";
        private const string AccessTokenCacheKey = "AccessToken";
        private static readonly TimeSpan AccessTokenCacheExpiry = TimeSpan.FromHours(1);
        private static string wechatProxyHost;
        private static string wechatPayHost;
        private readonly Lazy<ICacheStore> cacheStore;

        public WechatIntegration(Lazy<ICacheStore> cacheStore)
        {
            this.cacheStore = cacheStore;
        }

        public static void Initialize(string wechatReverseProxy, string wechatPayReverseProxy)
        {
            wechatProxyHost = wechatReverseProxy ?? throw new ArgumentNullException(nameof(wechatReverseProxy));
            wechatPayHost = wechatPayReverseProxy ?? throw new ArgumentNullException(nameof(wechatPayReverseProxy));
        }

        public async Task<OperationResult<string>> GenerateMediaDownloadUrl(string appId, string secret, string mediaID)
        {
            var token = await GetAccessToken(appId, secret);
            if (!token.IsSuccess)
            {
                return token;
            }

            return OperationResult<string>.Create($"http://{WechatHost}/cgi-bin/media/get?access_token={token.Result}&media_id={mediaID}");
        }

        public async Task<OperationResult<Stream>> GetMediaAsync(string appId, string secret, string mediaID)
        {
            var url = await this.GenerateMediaDownloadUrl(appId, secret, mediaID);
            if (!url.IsSuccess)
            {
                return OperationResult<Stream>.Create(url.Code, reference: url.Reference);
            }

            using (var httpclient = new HttpClient(HttpHandler.GetHandler(), false))
            {
                var resp = await httpclient.GetAsync(url.Result);
                var status = (int)resp.StatusCode;
                if (status >= 200 && status < 400)
                {
                    using (var s = await resp.Content.ReadAsStreamAsync())
                    {
                        var ms = new MemoryStream();
                        await s.CopyToAsync(ms);
                        return OperationResult<Stream>.Create(ms);
                    }
                }
                return OperationResult<Stream>.Create(status, null);
            }
        }

        public async Task<OperationResult<string>> PerformOCRAsync(string appId, string secret, WechatUploadKind kind, string mediaID)
        {
            var token = await GetAccessToken(appId, secret);
            if (!token.IsSuccess)
            {
                return token;
            }

            var path = GetOCRPath(kind);
            var query = HttpUtility.ParseQueryString(String.Empty);
            query["access_token"] = token.Result;
            query["img_url"] = $"https://{WechatHost}/cgi-bin/media/get?access_token={token.Result}&media_id={mediaID}";
            using (var httpclient = new HttpClient(HttpHandler.GetHandler(), false))
            {
                var resp = await httpclient.GetAsync($"https://{WechatHost}/{path}?{query.ToString()}");
                var status = (int)resp.StatusCode;
                var json = await resp.Content.ReadAsStringAsync();
                if (status >= 200 && status < 400)
                {
                    if (json.Contains("\"errcode\":0,"))
                    {
                        return OperationResult<string>.Create(json);
                    }
                    return OperationResult<string>.Create(InternalError.InternalServerError, json);
                }
                return OperationResult<string>.Create(status, json);
            }
        }

        public async Task<OperationResult<string>> PerformOCRAsync(string appId, string secret, WechatUploadKind kind, Stream input)
        {
            var token = await GetAccessToken(appId, secret);
            if (!token.IsSuccess)
            {
                return token;
            }

            var path = GetOCRPath(kind);
            var buff = new byte[512];
            var hash = Guid.NewGuid().ToString("N").Substring(0, 16).ToLowerInvariant();
            var request = (HttpWebRequest)WebRequest.Create($"https://{WechatHost}/{path}?access_token={token.Result}");
            request.Method = "POST";
            request.ContentType = $"multipart/form-data; boundary=------------------------{hash}";
            using (var requestStream = request.GetRequestStream())
            {
                using (var requestWriter = new StreamWriter(requestStream))
                {
                    var sb = new StringBuilder();
                    sb.Append($"--------------------------{hash}\r\n");
                    sb.Append($"Content-Disposition: form-data; name=\"img\"; filename=\"{hash}.jpg\"\r\n");
                    sb.Append("Content-Type: image/jpeg\r\n");
                    sb.Append("\r\n");
                    await requestWriter.WriteAsync(sb.ToString());
                }

                if (input.CanSeek)
                {
                    input.Seek(0, SeekOrigin.Begin);
                }

                while (true)
                {
                    var read = await input.ReadAsync(buff, 0, buff.Length);
                    if (read <= 0)
                    {
                        break;
                    }
                    else
                    {
                        await requestStream.WriteAsync(buff, 0, read);
                    }
                }

                using (var requestWriter = new StreamWriter(requestStream))
                {
                    await requestWriter.WriteAsync($"\r\n--------------------------{hash}--\r\n");
                }
            }

            var response = (HttpWebResponse)request.GetResponse();
            using (var responseStream = response.GetResponseStream())
            {
                using (var sr = new StreamReader(responseStream))
                {
                    var s = await sr.ReadToEndAsync();
                    if (s.Contains("\"errcode\":0,"))
                    {
                        return OperationResult<string>.Create(s);
                    }
                    return OperationResult<string>.Create(InternalError.InternalServerError, s);
                }
            }
        }

        public async Task<OperationResult<string>> GetJSApiTicket(string appID, string secret)
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
                var resp = await httpclient.GetAsync($"https://{WechatHost}/cgi-bin/ticket/getticket?{query.ToString()}");
                var status = (int)resp.StatusCode;
                var json = await resp.Content.ReadAsStringAsync();
                if (status >= 200 && status < 400)
                {
                    var result = JsonConvert.DeserializeObject<JsTicketResult>(json, JsonSetting.UnderstoreCaseSetting);
                    if (!String.IsNullOrWhiteSpace(result.Ticket))
                    {
                        return OperationResult<string>.Create(result.Ticket);
                    }
                    else
                    {
                        return OperationResult<string>.Create(result.Errcode, json, result.Errmsg);
                    }
                }
                return OperationResult<string>.Create(status, json);
            }
        }

        private async Task<OperationResult<string>> GetAccessToken(string appID, string secret)
        {
            var token = await cacheStore.Value.GetAsync<string>(appID, AccessTokenCacheKey);
            if (!String.IsNullOrWhiteSpace(token))
            {
                return OperationResult<string>.Create(token);
            }

            using (var httpclient = new HttpClient(HttpHandler.GetHandler(), false))
            {
                var query = HttpUtility.ParseQueryString(String.Empty);
                query["grant_type"] = "client_credential";
                query["appid"] = appID;
                query["secret"] = secret;
                var resp = await httpclient.GetAsync($"{wechatProxyHost}/cgi-bin/token?{query.ToString()}");
                var status = (int)resp.StatusCode;
                var json = await resp.Content.ReadAsStringAsync();
                if (status >= 200 && status < 400)
                {
                    var result = JsonConvert.DeserializeObject<TokenResult>(json, JsonSetting.UnderstoreCaseSetting);
                    if (!String.IsNullOrWhiteSpace(result.AccessToken))
                    {
                        await cacheStore.Value.SetAsync(appID, AccessTokenCacheKey, result.AccessToken, true, DateTimeOffset.UtcNow.Add(AccessTokenCacheExpiry));
                        return OperationResult<string>.Create(result.AccessToken);
                    }
                    else
                    {
                        return OperationResult<string>.Create(result.Errcode, json, result.Errmsg);
                    }
                }
                return OperationResult<string>.Create(status, json);
            }
        }

        public async Task<OperationResult<string>> SendNotificationAsync(string appId, string secret, string openId, string templateId, WechatNotificationParameter parameters, string link)
        {
            var token = await GetAccessToken(appId, secret);
            if (!token.IsSuccess)
            {
                return token;
            }

            using (var httpclient = new HttpClient(HttpHandler.GetHandler(), false))
            {
                var query = HttpUtility.ParseQueryString(String.Empty);
                query["access_token"] = token.Result;
                var request = new WechatTemplateMessageRequest
                {
                    Data = "JSON_DATA",
                    TemplateId = templateId,
                    Touser = openId,
                    Url = link
                };
                var data = JsonConvert.SerializeObject(request, JsonSetting.UnderstoreCaseSetting);
                data = data.Replace("JSON_DATA", parameters.ToJson());
                using (var content = new StringContent(data))
                {
                    var resp = await httpclient.PostAsync($"https://{WechatHost}/cgi-bin/message/template/send?{query.ToString()}", content);
                    var status = (int)resp.StatusCode;
                    var json = await resp.Content.ReadAsStringAsync();
                    if (status >= 200 && status < 400)
                    {
                        var result = JsonConvert.DeserializeObject<WechatTemplateMessageResponse>(json);
                        if (result.ErrCode != 0)
                        {
                            return OperationResult<string>.Create(result.ErrCode, json, result.ErrMsg);
                        }
                        else
                        {
                            return OperationResult<string>.Create(OperationResult.SuccessCode, json);
                        }
                    }
                    return OperationResult<string>.Create(status, json);
                }
            }
        }
        public async Task<OperationResult<string>> GetOpenIDAsync(string appID, string secret, string code)
        {
            using (var httpclient = new HttpClient(HttpHandler.GetHandler(), false))
            {
                var query = HttpUtility.ParseQueryString(String.Empty);
                query["appid"] = appID;
                query["secret"] = secret;
                query["code"] = code;
                query["grant_type"] = "authorization_code";
                var resp = await httpclient.GetAsync($"https://{WechatHost}/sns/oauth2/access_token?{query.ToString()}");
                var status = (int)resp.StatusCode;
                var json = await resp.Content.ReadAsStringAsync();
                if (status >= 200 && status < 400)
                {
                    var result = JsonConvert.DeserializeObject<OpenIdResult>(json);
                    if (!String.IsNullOrWhiteSpace(result.Openid))
                    {
                        return OperationResult<string>.Create(result.Openid);
                    }
                    else
                    {
                        return OperationResult<string>.Create(result.Errcode, json, result.Errmsg);
                    }
                }
                return OperationResult<string>.Create(status, json);
            }
        }

        public async Task<OperationResult<string>> JSAPIPay(string account, int amount, string appID, string device, string order, string product, string attach, string ip,
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
            var sign = MD5Sign(param, wechatMerchantSignature);
            param.Add("sign", sign);

            using (var httpclient = new HttpClient(HttpHandler.GetHandler(), false))
            {
                var resp = await httpclient.PostAsync($"https://{WechatPayHost}/pay/unifiedorder",
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

        public async Task<OperationResult<WechatUserInfo>> GetUserInfoAsync(string appId, string secret, string openID, string lang = "zh_CN")
        {
            var token = await GetAccessToken(appId, secret);
            if (!token.IsSuccess)
            {
                return OperationResult<WechatUserInfo>.Create(token.Code, token.Message);
            }

            var url = $"https://{WechatHost}/cgi-bin/user/info?access_token={token.Result}&openid={openID}&lang={lang}";
            using (var httpclient = new HttpClient(HttpHandler.GetHandler(), false))
            {
                var resp = await httpclient.GetAsync(url);
                var status = (int)resp.StatusCode;
                if (status >= 200 && status < 400)
                {
                    var json = await resp.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<WechatUserInfo>(json);
                    return OperationResult<WechatUserInfo>.Create(result);
                }
                return OperationResult<WechatUserInfo>.Create(status, null);
            }
        }

        public Dictionary<string, object> GetJSAPIPaySignature(string prepayID, string appID, string wechatMerchantSignature)
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
            var signature = MD5Sign(resultParam, wechatMerchantSignature);
            resultParam.Add("paySign", signature);
            return resultParam;
        }

        internal static Dictionary<string, string> FromXML(string xml)
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

        internal static string MD5Sign(Dictionary<string, object> param, string signKey)
        {
            var sorted = param.Select(kv => new KeyValuePair<string, string>(kv.Key, kv.Value.ToString())).OrderBy(p => p.Key);
            return MD5Sign(sorted, signKey);
        }

        internal static string MD5Sign(IOrderedEnumerable<KeyValuePair<string, string>> items, string signKey)
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

        private static string GetOCRPath(WechatUploadKind kind)
        {
            switch (kind)
            {
                case WechatUploadKind.Code:
                    return "cv/img/qrcode";
                case WechatUploadKind.OCR:
                    return "cv/ocr/comm";
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
