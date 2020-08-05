using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Cod.Platform
{
    public class BaiduIntegration
    {
        private const string Host = "https://aip.baidubce.com";

        private const string AccessTokenCacheKey = "AccessToken";
        private const string IntegrationKey = "BAIDU_APP_KEY";
        private const string IntegrationSecret = "BAIDU_APP_SECRET";

        private readonly Lazy<ICacheStore> cacheStore;
        private readonly Lazy<IConfigurationProvider> configuration;

        public BaiduIntegration(Lazy<ICacheStore> cacheStore, Lazy<IConfigurationProvider> configuration)
        {
            this.cacheStore = cacheStore;
            this.configuration = configuration;
        }

        public async Task<OperationResult<ChineseIDInfo>> AnalyzeChineseIDAsync(Stream frontCNID, Stream backCNID)
        {
            var srs = await this.AnalyzeChineseIDAsync(frontCNID, true);
            var kvs = new Dictionary<string, string>();
            if (!srs.IsSuccess)
            {
                return OperationResult<ChineseIDInfo>.Create(InternalError.InternalServerError, JsonConvert.SerializeObject(srs));
            }
            else
            {
                foreach (var i in srs.Result.WordsResult)
                {
                    kvs.Add(i.Key, i.Value.Words);
                }
            }

            srs = await this.AnalyzeChineseIDAsync(backCNID, false);
            if (!srs.IsSuccess)
            {
                return OperationResult<ChineseIDInfo>.Create(InternalError.InternalServerError, JsonConvert.SerializeObject(srs));
            }
            else
            {
                foreach (var i in srs.Result.WordsResult)
                {
                    kvs.Add(i.Key, i.Value.Words);
                }
            }

            var info = new ChineseIDInfo();

            // REMARK (wangzhiheng) 百度身份证识别返回的 key, 姓名、性别、民族、出生日期、住址、身份证号、签发机关、失效日期
            foreach (var i in kvs)
            {
                switch (i.Key.Trim())
                {
                    case "姓名":
                        info.Name = i.Value.Trim();
                        break;
                    case "性别":
                        info.Gender = i.Value.Trim().Equals("男", StringComparison.OrdinalIgnoreCase) ? Gender.Male : Gender.Female;
                        break;
                    case "民族":
                        info.Race = i.Value.Trim();
                        break;
                    case "出生":
                        var bd = i.Value.Trim().ParseDate();
                        if (bd.HasValue)
                        {
                            info.Birthday = bd.Value;
                        }

                        break;
                    case "住址":
                        info.Address = i.Value.Trim();
                        break;
                    case "公民身份号码":
                        info.Number = i.Value.Trim();
                        break;
                    case "签发机关":
                        info.Issuer = i.Value.Trim();
                        break;
                    case "失效日期":
                        var ed = i.Value.Trim().ParseDate();
                        if (ed.HasValue)
                        {
                            info.Expiry = ed.Value;
                        }

                        break;
                    default:
                        break;
                }
            }

            if (!ValidationHelper.TryValidate(info, out var result))
            {
                return OperationResult<ChineseIDInfo>.Create(InternalError.InternalServerError, JsonConvert.SerializeObject(result));
            }
            else
            {
                return OperationResult<ChineseIDInfo>.Create(info);
            }
        }

        public async Task<OperationResult<IEnumerable<CodeScanResult>>> ScanCodeAsync(Stream stream, int retry = 0)
        {
            if (retry > 3)
            {
                return OperationResult<IEnumerable<CodeScanResult>>.Create(InternalError.GatewayTimeout, null);
            }

            var token = await GetAccessToken();
            if (!token.IsSuccess)
            {
                return new OperationResult<IEnumerable<CodeScanResult>>(token);
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
                using (var httpclient = new HttpClient(HttpHandler.GetHandler(), false)
                {
#if !DEBUG
                    Timeout = TimeSpan.FromSeconds(3),
#endif
                })
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
                                return OperationResult<IEnumerable<CodeScanResult>>.Create(result.CodesResult.SelectMany(r =>
                                    r.Text.Select(t => new CodeScanResult
                                    {
                                        Code = t,
                                        Kind = r.Type == "CODE_128" ? CodeKind.CODE_128 : r.Type == "QR_CODE" ? CodeKind.QR_CODE : CodeKind.Unknown,
                                    })));
                            }
                            return OperationResult<IEnumerable<CodeScanResult>>.Create(InternalError.InternalServerError, json);
                        }
                        return OperationResult<IEnumerable<CodeScanResult>>.Create(status, json);
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

            return await ScanCodeAsync(stream, ++retry);
        }

        public async Task<OperationResult<IEnumerable<OCRScanResult>>> PerformOCRAsync(
            Uri mediaUri,
            Stream stream,
            bool tryHarder,
            int retry = 0)
        {
            if (retry > 3)
            {
                return OperationResult<IEnumerable<OCRScanResult>>.Create(InternalError.GatewayTimeout, null);
            }

            var token = await this.GetAccessToken();
            if (!token.IsSuccess)
            {
                return new OperationResult<IEnumerable<OCRScanResult>>(token);
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
                str += $"&url={WebUtility.UrlEncode(mediaUri.AbsoluteUri)}";
            }

            try
            {
                using (var httpclient = new HttpClient(HttpHandler.GetHandler(), false)
                {
#if !DEBUG
                    Timeout = TimeSpan.FromSeconds(2)
#endif
                })
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
                                return OperationResult<IEnumerable<OCRScanResult>>.Create(
                                result.WordsResult.Select(r => new OCRScanResult
                                {
                                    Text = r.Words,
                                    IsConfident = r.Probability.Variance < 0.019d // REMARK (5he11) 方差不能太大，否则识别不准确
                                     && r.Probability.Min > 0.6d // REMARK (5he11) 最小信心不能太小，否则识别不准确
                                }));
                            }
                            return OperationResult<IEnumerable<OCRScanResult>>.Create(InternalError.InternalServerError, json);
                        }
                        return OperationResult<IEnumerable<OCRScanResult>>.Create(status, json);
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

            return await PerformOCRAsync(mediaUri, stream, tryHarder, ++retry);
        }

        public async Task<OperationResult> CompareFaceAsync(Uri faceMediaUri, Uri frontCNIDMediaUri, float minScore = 80)
        {
            _ = faceMediaUri ?? throw new ArgumentNullException(nameof(faceMediaUri));
            _ = frontCNIDMediaUri ?? throw new ArgumentNullException(nameof(frontCNIDMediaUri));
            var rs = await this.CompareFaceAsync(faceMediaUri.AbsoluteUri, frontCNIDMediaUri.AbsoluteUri);
            if (rs.IsSuccess && rs.Result.Result.Score >= minScore)
            {
                return OperationResult.Create();
            }
            else
            {
                return OperationResult.Create(InternalError.InternalServerError, JsonConvert.SerializeObject(rs));
            }
        }

        public async Task<OperationResult<BaiduCompareFaceResponse>> CompareFaceAsync(string faceUrl, string frontCNIDUrl, int retry = 3)
        {
            if (retry <= 0)
            {
                return OperationResult<BaiduCompareFaceResponse>.Create(InternalError.GatewayTimeout, null);
            }

            var token = await this.GetAccessToken();
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
            return await CompareFaceAsync(faceUrl, frontCNIDUrl, --retry);
        }

        private async Task<OperationResult<string>> GetAccessToken(int retry = 0)
        {
            var key = await this.configuration.Value.GetSettingAsync(IntegrationKey);
            var token = await cacheStore.Value.GetAsync<string>(key, AccessTokenCacheKey);
            if (!String.IsNullOrWhiteSpace(token))
            {
                return OperationResult<string>.Create(token);
            }

            var secret = await this.configuration.Value.GetSettingAsync(IntegrationSecret);
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

                return await GetAccessToken(++retry);
            }
        }

        private async Task<OperationResult<BaiduIDScanResponse>> AnalyzeChineseIDAsync(Stream stream, bool isFrontSide, int retry = 3)
        {
            if (retry <= 0)
            {
                return OperationResult<BaiduIDScanResponse>.Create(InternalError.GatewayTimeout, null);
            }

            var token = await this.GetAccessToken();
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
                using (var httpclient = new HttpClient(HttpHandler.GetHandler(), false)
                {
#if !DEBUG
                    Timeout = TimeSpan.FromSeconds(3),
#endif
                })
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

            return await AnalyzeChineseIDAsync(stream, isFrontSide, --retry);
        }
    }
}
