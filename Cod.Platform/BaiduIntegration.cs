using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

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

        public async Task<OperationResult<BaiduIDScanResponse>> ScanIDAsync(string key, string secret, Stream stream, bool isFrontSide, int retry = 0)
        {
            if (retry > 3)
            {
                return OperationResult<BaiduIDScanResponse>.Create(InternalError.GatewayTimeout, null);
            }

            var token = await GetAccessToken(key, secret);
            if (!token.IsSuccess)
            {
                return OperationResult<BaiduIDScanResponse>.Create(token.Code, reference: token);
            }

            if (stream.CanSeek)
            {
                stream.Seek(0, SeekOrigin.Begin);
            }

            byte[] buff;
            using (var ms = new MemoryStream())
            {
                using (var image = Image.Load(stream))
                {
                    var ratio = image.Height / 400d;
                    image.Mutate(x => x.Resize((int)(image.Width / ratio), (int)(image.Height / ratio)));
                    image.SaveAsJpeg(ms);
                    buff = ms.ToByteArray();
                }
            }

            var sideParam = isFrontSide ? "front" : "back";
            var base64 = WebUtility.UrlEncode(Convert.ToBase64String(buff));
            var str = $"access_token={token.Result}&id_card_side={sideParam}&detect_direction=true&detect_risk=true&detect_rectify=true&image={base64}";

            try
            {
                using (var httpclient = new HttpClient(HttpHandler.GetHandler(), false) { Timeout = TimeSpan.FromSeconds(3) })
                {
                    using (var post = new StringContent(str, Encoding.UTF8, "application/x-www-form-urlencoded"))
                    {
                        var resp = await httpclient.PostAsync($"{Host}/rest/2.0/ocr/v1/idcard", post);
                        var status = (int)resp.StatusCode;
                        var json = await resp.Content.ReadAsStringAsync();
                        if (status >= 200 && status < 400)
                        {
                            var result = JsonConvert.DeserializeObject<BaiduIDScanResponse>(json, JsonSetting.UnderstoreCase);
                            if (!result.ErrorCode.HasValue)
                            {
                                return OperationResult<BaiduIDScanResponse>.Create(result);
                            }
                            return OperationResult<BaiduIDScanResponse>.Create(InternalError.InternalServerError, json);
                        }
                        return OperationResult<BaiduIDScanResponse>.Create(status, json);
                    }
                }
            }
            catch (TaskCanceledException)
            {
            }
            catch (SocketException)
            {
            }
            catch (IOException)
            {
            }

            return await ScanIDAsync(key, secret, stream, isFrontSide, ++retry);
        }

        public async Task<OperationResult<BaiduCodeScanResponse>> ScanCodeAsync(string key, string secret, Stream stream, int retry = 0)
        {
            if (retry > 3)
            {
                return OperationResult<BaiduCodeScanResponse>.Create(InternalError.GatewayTimeout, null);
            }

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
                using (var httpclient = new HttpClient(HttpHandler.GetHandler(), false) { Timeout = TimeSpan.FromSeconds(3) })
                {
                    using (var post = new StringContent(str, Encoding.UTF8, "application/x-www-form-urlencoded"))
                    {
                        var resp = await httpclient.PostAsync($"{Host}/rest/2.0/ocr/v1/qrcode", post);
                        var status = (int)resp.StatusCode;
                        var json = await resp.Content.ReadAsStringAsync();
                        if (status >= 200 && status < 400)
                        {
                            var result = JsonConvert.DeserializeObject<BaiduCodeScanResponse>(json, JsonSetting.UnderstoreCase);
                            if (!result.ErrorCode.HasValue)
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
            }
            catch (SocketException)
            {
            }
            catch (IOException)
            {
            }

            return await ScanCodeAsync(key, secret, stream, ++retry);
        }

        public async Task<OperationResult<BaiduOCRResponse>> PerformOCRAsync(
            string key,
            string secret,
            string mediaURL,
            Stream stream,
            bool tryHarder,
            int retry = 0)
        {
            if (retry > 3)
            {
                return OperationResult<BaiduOCRResponse>.Create(InternalError.GatewayTimeout, null);
            }

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
                using (var httpclient = new HttpClient(HttpHandler.GetHandler(), false) { Timeout = TimeSpan.FromSeconds(2) })
                {
                    using (var post = new StringContent(str, Encoding.UTF8, "application/x-www-form-urlencoded"))
                    {
                        var path = tryHarder ? "rest/2.0/ocr/v1/accurate_basic" : "rest/2.0/ocr/v1/general_basic";
                        var resp = await httpclient.PostAsync($"{Host}/{path}", post);
                        var status = (int)resp.StatusCode;
                        var json = await resp.Content.ReadAsStringAsync();
                        if (status >= 200 && status < 400)
                        {
                            var result = JsonConvert.DeserializeObject<BaiduOCRResponse>(json, JsonSetting.UnderstoreCase);
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
            }
            catch (SocketException)
            {
            }
            catch (IOException)
            {
            }

            return await PerformOCRAsync(key, secret, mediaURL, stream, tryHarder, ++retry);
        }

        public async Task<OperationResult<BaiduCompareFaceResponse>> CompareFaceAsync(string key, string secret, string faceUrl, string frontCNIDUrl, int retry = 0)
        {
            if (retry > 3)
            {
                return OperationResult<BaiduCompareFaceResponse>.Create(InternalError.GatewayTimeout, null);
            }

            var token = await GetAccessToken(key, secret);
            if (!token.IsSuccess)
            {
                return OperationResult<BaiduCompareFaceResponse>.Create(token.Code, reference: token);
            }

            try
            {
                using (var httpClient = new HttpClient(HttpHandler.GetHandler(), false))
                {
                    var url = $"{Host}/rest/2.0/face/v3/match?access_token={token.Result}";

                    var content = new List<Dictionary<string, string>>()
                    {
                        new Dictionary<string, string>()
                        {
                            { "image", faceUrl},
                            { "image_type", "URL"},
                            { "face_type", "LIVE" },
                            { "quality_control", "NORMAL" },
                        },
                        new Dictionary<string, string>()
                        {
                            { "image", frontCNIDUrl},
                            { "image_type", "URL"},
                            { "face_type", "CERT" },
                            { "quality_control", "NORMAL" },
                        }
                    };
                    var js = JsonConvert.SerializeObject(content);
                    using (var post = new StringContent(js, Encoding.UTF8, "application/json"))
                    {
                        var response = await httpClient.PostAsync(url, post);
                        var statusCode = (int)response.StatusCode;
                        if (statusCode >= 200 && statusCode < 400)
                        {
                            js = await response.Content.ReadAsStringAsync();
                            var result = JsonConvert.DeserializeObject<BaiduCompareFaceResponse>(js, JsonSetting.UnderstoreCase);
                            if (!result.ErrorCode.HasValue || result.ErrorCode == 0)
                            {
                                return OperationResult<BaiduCompareFaceResponse>.Create(result);
                            }
                            else
                            {
                                return OperationResult<BaiduCompareFaceResponse>.Create(InternalError.InternalServerError, js);
                            }
                        }
                        else
                        {
                            return OperationResult<BaiduCompareFaceResponse>.Create(statusCode, js);
                        }
                    }
                }
            }
            catch
            {

            }
            return await CompareFaceAsync(key, secret, faceUrl, frontCNIDUrl, retry++);
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
                            var result = JsonConvert.DeserializeObject<BaiduAccessTokenResponse>(json, JsonSetting.UnderstoreCase);
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
