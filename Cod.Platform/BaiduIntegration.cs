using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Cod.Platform
{
    public class BaiduIntegration
    {
        private const string Host = "https://aip.baidubce.com";
        private const string AccessTokenCacheKey = "AccessToken";
        private readonly Lazy<ICacheStore> cacheStore;

        public BaiduIntegration(Lazy<ICacheStore> cacheStore)
        {
            this.cacheStore = cacheStore;
        }

        public async Task<OperationResult<BaiduCodeScanResponse>> ScanCodeAsync(string key, string secret, Stream stream)
        {
            var token = await GetAccessToken(key, secret);
            if (!token.IsSuccess)
            {
                return OperationResult<BaiduCodeScanResponse>.Create(token.Code, reference: token);
            }

            byte[] data;
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                data = ms.ToArray();
            }
            var base64 = WebUtility.UrlEncode(Convert.ToBase64String(data));

            var str = $"access_token={token.Result}&image={base64}";
            using (var httpclient = new HttpClient(HttpHandler.GetHandler(), false))
            {
                using (var post = new StringContent(str, System.Text.Encoding.UTF8, "application/x-www-form-urlencoded"))
                {
                    var resp = await httpclient.PostAsync($"{Host}/rest/2.0/ocr/v1/qrcode", post);
                    var status = (int)resp.StatusCode;
                    var json = await resp.Content.ReadAsStringAsync();
                    if (status >= 200 && status < 400)
                    {
                        var result = JsonConvert.DeserializeObject<BaiduCodeScanResponse>(json, JsonSetting.UnderstoreCaseSetting);
                        if (result.CodesResult != null && result.CodesResult.Length > 0)
                        {
                            return OperationResult<BaiduCodeScanResponse>.Create(result);
                        }
                        return OperationResult<BaiduCodeScanResponse>.Create(InternalError.InternalServerError, json);
                    }
                    return OperationResult<BaiduCodeScanResponse>.Create(status, json);
                }
            }
        }

        public async Task<OperationResult<BaiduOCRResponse>> PerformOCRAsync(string key, string secret, string mediaURL, bool tryHarder)
        {
            var token = await GetAccessToken(key, secret);
            if (!token.IsSuccess)
            {
                return OperationResult<BaiduOCRResponse>.Create(token.Code, reference: token);
            }

            using (var httpclient = new HttpClient(HttpHandler.GetHandler(), false))
            {
                using (var post = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "access_token", token.Result },
                    { "url", mediaURL },
                    { "detect_direction", "true" },
                    { "probability", "true" },
                }))
                {
                    var path = tryHarder ? "rest/2.0/ocr/v1/accurate_basic" : "rest/2.0/ocr/v1/general_basic";
                    var resp = await httpclient.PostAsync($"{Host}/{path}", post);
                    var status = (int)resp.StatusCode;
                    var json = await resp.Content.ReadAsStringAsync();
                    if (status >= 200 && status < 400)
                    {
                        var result = JsonConvert.DeserializeObject<BaiduOCRResponse>(json, JsonSetting.UnderstoreCaseSetting);
                        if (result.WordsResult != null && result.WordsResult.Length > 0)
                        {
                            return OperationResult<BaiduOCRResponse>.Create(result);
                        }
                        return OperationResult<BaiduOCRResponse>.Create(InternalError.InternalServerError, json);
                    }
                    return OperationResult<BaiduOCRResponse>.Create(status, json);
                }
            }
        }

        private async Task<OperationResult<string>> GetAccessToken(string key, string secret)
        {
            var token = await cacheStore.Value.GetAsync<string>(key, AccessTokenCacheKey);
            if (!String.IsNullOrWhiteSpace(token))
            {
                return OperationResult<string>.Create(token);
            }

            using (var httpclient = new HttpClient(HttpHandler.GetHandler(), false))
            {
                using (var post = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "grant_type", "client_credentials" },
                    { "client_id", key },
                    { "client_secret", secret },
                }))
                {
                    var resp = await httpclient.PostAsync($"{Host}/oauth/2.0/token", post);
                    var status = (int)resp.StatusCode;
                    var json = await resp.Content.ReadAsStringAsync();
                    if (status >= 200 && status < 400)
                    {
                        var result = JsonConvert.DeserializeObject<BaiduAccessTokenResponse>(json, JsonSetting.UnderstoreCaseSetting);
                        if (!String.IsNullOrWhiteSpace(result.AccessToken))
                        {
                            await cacheStore.Value.SetAsync(key, AccessTokenCacheKey, result.AccessToken, true, DateTimeOffset.UtcNow.Add(result.GetExpiry()));
                            return OperationResult<string>.Create(result.AccessToken);
                        }
                        else
                        {
                            return OperationResult<string>.Create(InternalError.InternalServerError, result, result.ErrorDescription);
                        }
                    }
                    return OperationResult<string>.Create(status, json);
                }
            }
        }
    }
}
