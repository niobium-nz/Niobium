using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
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

        public async Task<OperationResult<BaiduCodeScanResponse>> ScanCodeAsync(string key, string secret, Stream stream, int retry = 0)
        {
            var token = await GetAccessToken(key, secret);
            if (!token.IsSuccess)
            {
                return OperationResult<BaiduCodeScanResponse>.Create(token.Code, reference: token);
            }

            if (stream.CanSeek)
            {
                stream.Seek(0, SeekOrigin.Begin);
            }

            var buff = stream.ToByteArray();
            var base64 = WebUtility.UrlEncode(Convert.ToBase64String(buff));
            var str = $"access_token={token.Result}&image={base64}";

            try
            {
                using (var httpclient = new HttpClient(HttpHandler.GetHandler(), false) { Timeout = TimeSpan.FromSeconds(5) })
                {
                    using (var post = new StringContent(str, Encoding.UTF8, "application/x-www-form-urlencoded"))
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
            catch (TaskCanceledException)
            {
                if (retry > 3)
                {
                    return OperationResult<BaiduCodeScanResponse>.Create(InternalError.GatewayTimeout, null);
                }

                return await ScanCodeAsync(key, secret, stream, ++retry);
            }

        }

        public async Task<OperationResult<BaiduOCRResponse>> PerformOCRAsync(
            string key,
            string secret,
            string mediaURL,
            Stream stream,
            bool tryHarder,
            int retry = 0)
        {
            var token = await GetAccessToken(key, secret);
            if (!token.IsSuccess)
            {
                return OperationResult<BaiduOCRResponse>.Create(token.Code, reference: token);
            }

            var str = $"access_token={token.Result}&detect_direction=true&probability=true";
            if (tryHarder)
            {
                if (stream.CanSeek)
                {
                    stream.Seek(0, SeekOrigin.Begin);
                }

                var buff = stream.ToByteArray();
                var base64 = WebUtility.UrlEncode(Convert.ToBase64String(buff));
                str += $"&image={base64}";
            }
            else
            {
                str += $"&url={WebUtility.UrlEncode(mediaURL)}";
            }

            try
            {
                using (var httpclient = new HttpClient(HttpHandler.GetHandler(), false) { Timeout = TimeSpan.FromSeconds(5) })
                {
                    using (var post = new StringContent(str, Encoding.UTF8, "application/x-www-form-urlencoded"))
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
            catch (TaskCanceledException)
            {
                if (retry > 3)
                {
                    return OperationResult<BaiduOCRResponse>.Create(InternalError.GatewayTimeout, null);
                }

                return await PerformOCRAsync(key, secret, mediaURL, stream, tryHarder, ++retry);
            }
        }

        private async Task<OperationResult<string>> GetAccessToken(string key, string secret, int retry = 0)
        {
            var token = await cacheStore.Value.GetAsync<string>(key, AccessTokenCacheKey);
            if (!String.IsNullOrWhiteSpace(token))
            {
                return OperationResult<string>.Create(token);
            }

            try
            {
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
            catch (TaskCanceledException)
            {
                if (retry > 3)
                {
                    return OperationResult<string>.Create(InternalError.GatewayTimeout, null);
                }

                return await GetAccessToken(key, secret, ++retry);
            }
        }
    }
}
