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
using Cod.Platform.Integration.Wechat;
using Cod.Platform.Model.Wechat;
using Microsoft.Extensions.Logging;

namespace Cod.Platform
{
    public class WechatIntegration
    {
        private const string WechatHost = "api.weixin.qq.com";
        private const string WechatPayHost = "api.mch.weixin.qq.com";
        private const string AccessTokenCacheKey = "AccessToken";
        private static readonly TimeSpan AccessTokenCacheExpiry = TimeSpan.FromHours(1);
        private static string wechatProxyHost;
        private static string memoryCachedAccessToken;
        private readonly Lazy<ICacheStore> cacheStore;

        public WechatIntegration(Lazy<ICacheStore> cacheStore) => this.cacheStore = cacheStore;

        public static void Initialize(string wechatReverseProxy) => wechatProxyHost = wechatReverseProxy ?? throw new ArgumentNullException(nameof(wechatReverseProxy));

        public async Task<OperationResult<Uri>> GenerateMediaUri(string appId, string secret, string mediaID)
        {
            var token = await this.GetAccessTokenAsync(appId, secret);
            if (!token.IsSuccess)
            {
                return new OperationResult<Uri>(token);
            }

            return new OperationResult<Uri>(new Uri($"https://{WechatHost}/cgi-bin/media/get?access_token={token.Result}&media_id={mediaID}"));
        }

        public async Task<OperationResult<WechatMediaSource>> GetMediaAsync(string appId, string secret, string mediaID, int retry = 0)
        {
            if (retry >= 3)
            {
                return new OperationResult<WechatMediaSource>(InternalError.InternalServerError);
            }

            var url = await this.GenerateMediaUri(appId, secret, mediaID);
            if (!url.IsSuccess)
            {
                return new OperationResult<WechatMediaSource>(url);
            }

            var result = await url.Result.FetchStreamAsync(null, 1);
            if (result == null)
            {
                return new OperationResult<WechatMediaSource>(InternalError.GatewayTimeout);
            }

            if (result.Length > 128)
            {
                return new OperationResult<WechatMediaSource>(new WechatMediaSource
                {
                    MediaStream = result,
                    MediaUri = url.Result,
                });
            }

            using (var sr = new StreamReader(result))
            {
                var err = await sr.ReadToEndAsync();
                if (err.Contains("\"errcode\":40001,"))
                {
                    await this.RevokeAccessTokenAsync(appId);
                    return await this.GetMediaAsync(appId, secret, mediaID, ++retry);
                }

                if (Logger.Instance != null)
                {
                    Logger.Instance.LogError($"An error occurred while trying to download media {mediaID} from Wechat: {err}");
                }
            }

            return new OperationResult<WechatMediaSource>(InternalError.BadGateway);
        }

        public async Task<OperationResult<IEnumerable<CodeScanResult>>> ScanCodeAsync(string appId, string secret, string mediaID)
        {
            var token = await this.GetAccessTokenAsync(appId, secret);
            if (!token.IsSuccess)
            {
                return new OperationResult<IEnumerable<CodeScanResult>>(token);
            }

            var path = GetOCRPath(WechatUploadKind.Code);
            var query = HttpUtility.ParseQueryString(String.Empty);
            query["access_token"] = token.Result;
            query["img_url"] = $"https://{WechatHost}/cgi-bin/media/get?access_token={token.Result}&media_id={mediaID}";
            using var httpclient = new HttpClient(HttpHandler.GetHandler(), false);
            var resp = await httpclient.PostAsync($"https://{WechatHost}/{path}?{query}", null);
            var status = (int)resp.StatusCode;
            var json = await resp.Content.ReadAsStringAsync();
            if (status >= 200 && status < 400)
            {
                if (json.Contains("\"errcode\":0,"))
                {
                    var result = JsonSerializer.DeserializeObject<WechatCodeScanResultList>(json, JsonSerializationFormat.UnderstoreCase);
                    return new OperationResult<IEnumerable<CodeScanResult>>(result.CodeResults.Select(r => new CodeScanResult
                    {
                        Code = r.Data,
                        Kind = r.TypeName == "CODE_128" ? CodeKind.CODE_128 : r.TypeName == "QR_CODE" ? CodeKind.QR_CODE : CodeKind.Unknown,
                    }));
                }

                if (json.Contains("\"errcode\":40001,"))
                {
                    await this.RevokeAccessTokenAsync(appId);
                    return await this.ScanCodeAsync(appId, secret, mediaID);
                }

                if (Logger.Instance != null)
                {
                    Logger.Instance.LogError($"An error occurred while trying to scan of code over media {mediaID} from Wechat with status code={status}: {json}");
                }
                return new OperationResult<IEnumerable<CodeScanResult>>(InternalError.BadGateway) { Reference = json };
            }
            return new OperationResult<IEnumerable<CodeScanResult>>(status) { Reference = json };
        }

        public async Task<OperationResult<string>> PerformOCRAsync(string appId, string secret, string mediaID)
        {
            var token = await this.GetAccessTokenAsync(appId, secret);
            if (!token.IsSuccess)
            {
                return token;
            }

            var path = GetOCRPath(WechatUploadKind.OCR);
            var query = HttpUtility.ParseQueryString(String.Empty);
            query["access_token"] = token.Result;
            query["img_url"] = $"https://{WechatHost}/cgi-bin/media/get?access_token={token.Result}&media_id={mediaID}";
            using var httpclient = new HttpClient(HttpHandler.GetHandler(), false);
            var resp = await httpclient.PostAsync($"https://{WechatHost}/{path}?{query}", null);
            var status = (int)resp.StatusCode;
            var json = await resp.Content.ReadAsStringAsync();
            if (status >= 200 && status < 400)
            {
                if (json.Contains("\"errcode\":0,"))
                {
                    return new OperationResult<string>(json);
                }

                if (json.Contains("\"errcode\":40001,"))
                {
                    await this.RevokeAccessTokenAsync(appId);
                    return await this.PerformOCRAsync(appId, secret, mediaID);
                }

                if (Logger.Instance != null)
                {
                    Logger.Instance.LogError($"An error occurred while trying to Perform OCR over media {mediaID} from Wechat with status code={status}: {json}");
                }
                return new OperationResult<string>(InternalError.BadGateway) { Reference = json };
            }
            return new OperationResult<string>(status) { Reference = json };
        }

        public async Task<OperationResult<string>> PerformOCRAsync(string appId, string secret, WechatUploadKind kind, Stream input)
        {
            var token = await this.GetAccessTokenAsync(appId, secret);
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
            using var responseStream = response.GetResponseStream();
            using var sr = new StreamReader(responseStream);
            var s = await sr.ReadToEndAsync();
            if (s.Contains("\"errcode\":0,"))
            {
                return new OperationResult<string>(s);
            }

            if (s.Contains("\"errcode\":40001,"))
            {
                await this.RevokeAccessTokenAsync(appId);
                return await this.PerformOCRAsync(appId, secret, kind, input);
            }

            if (Logger.Instance != null)
            {
                Logger.Instance.LogError($"An error occurred while trying to Perform OCR over stream media with status code={(int)response.StatusCode}: {s}");
            }
            return new OperationResult<string>(InternalError.BadGateway) { Reference = s };
        }

        public async Task<OperationResult<string>> GetJSApiTicket(string appID, string secret)
        {
            var token = await this.GetAccessTokenAsync(appID, secret);
            if (!token.IsSuccess)
            {
                return token;
            }

            using var httpclient = new HttpClient(HttpHandler.GetHandler(), false);
            var query = HttpUtility.ParseQueryString(String.Empty);
            query["access_token"] = token.Result;
            query["type"] = "jsapi";
            var resp = await httpclient.GetAsync($"https://{WechatHost}/cgi-bin/ticket/getticket?{query}");
            var status = (int)resp.StatusCode;
            var json = await resp.Content.ReadAsStringAsync();
            if (status >= 200 && status < 400)
            {
                var result = JsonSerializer.DeserializeObject<JsTicketResult>(json, JsonSerializationFormat.UnderstoreCase);
                if (!String.IsNullOrWhiteSpace(result.Ticket))
                {
                    return new OperationResult<string>(result.Ticket);
                }

                if (json.Contains("\"errcode\":40001,"))
                {
                    await this.RevokeAccessTokenAsync(appID);
                    return await this.GetJSApiTicket(appID, secret);
                }

                if (Logger.Instance != null)
                {
                    Logger.Instance.LogError($"An error occurred while trying to get JSAPI ticket for {appID} with status code={status}: {json}");
                }

                return new OperationResult<string>(InternalError.BadGateway) { Reference = json };
            }
            return new OperationResult<string>(status) { Reference = json };
        }

        private async Task<OperationResult<string>> GetAccessTokenAsync(string appID, string secret)
        {
            if (!String.IsNullOrWhiteSpace(memoryCachedAccessToken))
            {
                return new OperationResult<string>(memoryCachedAccessToken);
            }

            var token = await this.cacheStore.Value.GetAsync<string>(appID, AccessTokenCacheKey);
            if (!String.IsNullOrWhiteSpace(token))
            {
                return new OperationResult<string>(token);
            }

            using var httpclient = new HttpClient(HttpHandler.GetHandler(), false);
            var query = HttpUtility.ParseQueryString(String.Empty);
            query["grant_type"] = "client_credential";
            query["appid"] = appID;
            query["secret"] = secret;
            var resp = await httpclient.GetAsync($"{wechatProxyHost}/cgi-bin/token?{query}");
            var status = (int)resp.StatusCode;
            var json = await resp.Content.ReadAsStringAsync();
            if (status >= 200 && status < 400)
            {
                var result = JsonSerializer.DeserializeObject<TokenResult>(json, JsonSerializationFormat.UnderstoreCase);
                if (!String.IsNullOrWhiteSpace(result.AccessToken))
                {
                    await this.cacheStore.Value.SetAsync(appID, AccessTokenCacheKey, result.AccessToken, true, DateTimeOffset.UtcNow.Add(AccessTokenCacheExpiry));
                    return new OperationResult<string>(result.AccessToken);
                }
                else
                {
                    if (Logger.Instance != null)
                    {
                        Logger.Instance.LogError($"An error occurred while trying to aquire Wechat access token for {appID} with status code={status}: {json}");
                    }
                    return new OperationResult<string>(InternalError.BadGateway) { Reference = json };
                }
            }

            if (Logger.Instance != null)
            {
                Logger.Instance.LogError($"An error occurred while trying to aquire Wechat access token for {appID} with status code={status}: {json}");
            }
            return new OperationResult<string>(status) { Reference = json };
        }

        private async Task RevokeAccessTokenAsync(string appID)
        {
            memoryCachedAccessToken = null;
            await this.cacheStore.Value.DeleteAsync(appID, AccessTokenCacheKey);
        }

        public async Task<OperationResult<string>> SendNotificationAsync(string appId, string secret, string openId, string templateId, WechatNotificationParameter parameters, string link)
        {
            var token = await this.GetAccessTokenAsync(appId, secret);
            if (!token.IsSuccess)
            {
                return token;
            }

            using var httpclient = new HttpClient(HttpHandler.GetHandler(), false);
            var query = HttpUtility.ParseQueryString(String.Empty);
            query["access_token"] = token.Result;
            var request = new WechatTemplateMessageRequest
            {
                Data = "JSON_DATA",
                TemplateId = templateId,
                Touser = openId,
                Url = link
            };
            var data = JsonSerializer.SerializeObject(request, JsonSerializationFormat.UnderstoreCase);
            data = data.Replace("\"JSON_DATA\"", parameters.ToJson());
            using var content = new StringContent(data);
            var resp = await httpclient.PostAsync($"https://{WechatHost}/cgi-bin/message/template/send?{query}", content);
            var status = (int)resp.StatusCode;
            var json = await resp.Content.ReadAsStringAsync();
            if (status >= 200 && status < 400)
            {
                var result = JsonSerializer.DeserializeObject<WechatTemplateMessageResponse>(json);
                if (result.ErrCode != 0)
                {
                    if (result.ErrCode == 40001)
                    {
                        await this.RevokeAccessTokenAsync(appId);
                        return await this.SendNotificationAsync(appId, secret, openId, templateId, parameters, link);
                    }

                    if (Logger.Instance != null)
                    {
                        Logger.Instance.LogError($"An error occurred while trying to send Wechat notification to {openId} on {appId} with status code={status}: {json}");
                    }

                    return new OperationResult<string>(InternalError.BadGateway) { Reference = json };
                }
                else
                {
                    return new OperationResult<string>(json);
                }
            }

            if (Logger.Instance != null)
            {
                Logger.Instance.LogError($"An error occurred while trying to send Wechat notification to {openId} on {appId} with status code={status}: {json}");
            }

            return new OperationResult<string>(status) { Reference = json };
        }

        public async Task<OperationResult<string>> GetOpenIDAsync(string appID, string secret, string code)
        {
            using var httpclient = new HttpClient(HttpHandler.GetHandler(), false);
            var query = HttpUtility.ParseQueryString(String.Empty);
            query["appid"] = appID;
            query["secret"] = secret;
            query["code"] = code;
            query["grant_type"] = "authorization_code";
            var resp = await httpclient.GetAsync($"https://{WechatHost}/sns/oauth2/access_token?{query}");
            var status = (int)resp.StatusCode;
            var json = await resp.Content.ReadAsStringAsync();
            if (status >= 200 && status < 400)
            {
                var result = JsonSerializer.DeserializeObject<OpenIdResult>(json);
                if (!String.IsNullOrWhiteSpace(result.Openid))
                {
                    return new OperationResult<string>(result.Openid);
                }

                if (json.Contains("\"errcode\":40001,"))
                {
                    await this.RevokeAccessTokenAsync(appID);
                    return await this.GetOpenIDAsync(appID, secret, code);
                }

                if (Logger.Instance != null)
                {
                    Logger.Instance.LogError($"An error occurred while trying to get Wechat open ID for {appID} with status code={status}: {json}");
                }

                return new OperationResult<string>(InternalError.BadGateway) { Reference = json };
            }
            return new OperationResult<string>(status) { Reference = json };
        }

        public async Task<OperationResult<WechatUserInfo>> GetUserInfoAsync(string appId, string secret, string openID, string lang = "zh_CN")
        {
            var token = await this.GetAccessTokenAsync(appId, secret);
            if (!token.IsSuccess)
            {
                return new OperationResult<WechatUserInfo>(token);
            }

            var url = $"https://{WechatHost}/cgi-bin/user/info?access_token={token.Result}&openid={openID}&lang={lang}";
            using var httpclient = new HttpClient(HttpHandler.GetHandler(), false);
            var resp = await httpclient.GetAsync(url);
            var json = await resp.Content.ReadAsStringAsync();
            var status = (int)resp.StatusCode;
            if (status >= 200 && status < 400)
            {

                if (json.Contains("\"errcode\":40001,"))
                {
                    await this.RevokeAccessTokenAsync(appId);
                    return await this.GetUserInfoAsync(appId, secret, openID, lang);
                }

                if (Logger.Instance != null)
                {
                    Logger.Instance.LogError($"An error occurred while trying to get user info for {openID} with status code={status}: {json}");
                }

                var result = JsonSerializer.DeserializeObject<WechatUserInfo>(json);
                return new OperationResult<WechatUserInfo>(result);
            }
            return new OperationResult<WechatUserInfo>(status) { Reference = json };
        }

        internal async Task<OperationResult<string>> JSAPIPay(string account, int amount, string appID, string device, string reference, string desc, string attach, string ip,
                    string wechatMerchantID, string wechatMerchantNotifyUri, string wechatMerchantSignature, int retry = 0)
        {
            if (retry > 10)
            {
                return new OperationResult<string>(InternalError.Conflict);
            }

            if (retry == 1)
            {
                reference += retry.ToString();
            }
            else if (retry > 1)
            {
                reference = $"{reference.Substring(0, reference.Length - 1)}{retry}";
            }

            var nonceStr = Guid.NewGuid().ToString("N").ToUpperInvariant();
            var param = new Dictionary<string, object>
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
            var sign = MD5Sign(param, wechatMerchantSignature);
            param.Add("sign", sign);

            using var httpclient = new HttpClient(HttpHandler.GetHandler(), false);
            var xml = GetXML(param);
            if (Logger.Instance != null)
            {
                Logger.Instance.LogInformation($"微信支付调试: attach={attach} device={device} order={reference} xml={xml} retry={retry}");
            }

            var resp = await httpclient.PostAsync($"https://{WechatPayHost}/pay/unifiedorder",
                new StringContent(GetXML(param), Encoding.UTF8, "application/xml"));
            var status = (int)resp.StatusCode;
            var body = await resp.Content.ReadAsStringAsync();
            if (status < 200 || status >= 400)
            {
                return new OperationResult<string>(status) { Reference = body };
            }

            var result = FromXML(body);
            if (result["return_code"].ToUpperInvariant() == "SUCCESS" && result["result_code"].ToUpperInvariant() == "SUCCESS")
            {
                return new OperationResult<string>(result["prepay_id"]);
            }
            else if (result.ContainsKey("err_code_des") && result["err_code_des"].Contains("订单号重复"))
            {
                return await this.JSAPIPay(
                    account, amount, appID, device, reference, desc, attach, ip, wechatMerchantID, wechatMerchantNotifyUri, wechatMerchantSignature, ++retry);
            }

            return new OperationResult<string>(InternalError.BadGateway) { Reference = body };
        }

        internal Dictionary<string, object> GetJSAPIPaySignature(string prepayID, string appID, string wechatMerchantSignature)
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
            using var md5 = MD5.Create();
            var bs = md5.ComputeHash(Encoding.UTF8.GetBytes(tosign));
            var sb = new StringBuilder();
            foreach (var b in bs)
            {
                sb.Append(b.ToString("x2"));
            }
            var sign = sb.ToString().ToUpper();
            return sign;
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

        private static string GetOCRPath(WechatUploadKind kind) => kind switch
        {
            WechatUploadKind.Code => "cv/img/qrcode",
            WechatUploadKind.OCR => "cv/ocr/comm",
            _ => throw new NotImplementedException(),
        };
    }
}
