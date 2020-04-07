using System;
using System.Collections.Generic;
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

        public async Task<OperationResult<BaiduOCRResponse>> PerformOCRAsync(string key, string secret, string mediaURL)
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
                    var resp = await httpclient.PostAsync($"{Host}/rest/2.0/ocr/v1/general_basic", post);
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
