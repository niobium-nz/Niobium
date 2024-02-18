using Cod.Platform.Database;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Cod.Platform.OCR.Baidu
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
            OperationResult<BaiduIDScanResponse> srs = await AnalyzeChineseIDAsync(frontCNID, true);
            Dictionary<string, string> kvs = new();
            if (!srs.IsSuccess)
            {
                return new OperationResult<ChineseIDInfo>(srs);
            }
            else
            {
                foreach (KeyValuePair<string, BaiduOCRResult> i in srs.Result.WordsResult)
                {
                    if (kvs.ContainsKey(i.Key))
                    {
                        kvs[i.Key] = i.Value.Words;
                    }
                    else
                    {
                        kvs.Add(i.Key, i.Value.Words);
                    }
                }
            }

            srs = await AnalyzeChineseIDAsync(backCNID, false);
            if (!srs.IsSuccess)
            {
                return new OperationResult<ChineseIDInfo>(srs);
            }
            else
            {
                foreach (KeyValuePair<string, BaiduOCRResult> i in srs.Result.WordsResult)
                {
                    if (kvs.ContainsKey(i.Key))
                    {
                        kvs[i.Key] = i.Value.Words;
                    }
                    else
                    {
                        kvs.Add(i.Key, i.Value.Words);
                    }
                }
            }

            ChineseIDInfo info = new();

            // REMARK (wangzhiheng) 百度身份证识别返回的 key, 姓名、性别、民族、出生日期、住址、身份证号、签发机关、失效日期
            foreach (KeyValuePair<string, string> i in kvs)
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
                        DateTimeOffset? bd = i.Value.Trim().ParseDate();
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
                        if (i.Value.Trim().Contains("长期"))
                        {
                            info.Expiry = DateTimeOffsetExtensions.MaxValueForTableStorage;
                        }
                        else
                        {
                            DateTimeOffset? ed = i.Value.Trim().ParseDate();
                            if (ed.HasValue)
                            {
                                info.Expiry = ed.Value;
                            }
                        }

                        break;
                    default:
                        break;
                }
            }

            return !info.TryValidate(out ValidationState result)
                ? new OperationResult<ChineseIDInfo>(Cod.InternalError.BadRequest) { Reference = result }
                : new OperationResult<ChineseIDInfo>(info);
        }

        public async Task<OperationResult<IEnumerable<CodeScanResult>>> ScanCodeAsync(Stream stream, int retry = 0)
        {
            if (retry > 3)
            {
                return new OperationResult<IEnumerable<CodeScanResult>>(Cod.InternalError.GatewayTimeout);
            }

            OperationResult<string> token = await GetAccessToken();
            if (!token.IsSuccess)
            {
                return new OperationResult<IEnumerable<CodeScanResult>>(token);
            }

            if (stream.CanSeek)
            {
                stream.Seek(0, SeekOrigin.Begin);
            }

            byte[] buff = stream.ToByteArray();
            string base64 = WebUtility.UrlEncode(Convert.ToBase64String(buff));
            string str = $"access_token={token.Result}&image={base64}";

            try
            {
                using HttpClient httpclient = new(HttpHandler.GetHandler(), false)
                {
#if !DEBUG
                    Timeout = TimeSpan.FromSeconds(5),
#endif
                };
                using StringContent post = new(str, Encoding.UTF8, "application/x-www-form-urlencoded");
                HttpResponseMessage resp = await httpclient.PostAsync($"{Host}/rest/2.0/ocr/v1/qrcode", post);
                int status = (int)resp.StatusCode;
                string json = await resp.Content.ReadAsStringAsync();
                if (status is >= 200 and < 400)
                {
                    BaiduCodeScanResponse result = JsonSerializer.DeserializeObject<BaiduCodeScanResponse>(json, JsonSerializationFormat.UnderstoreCase);
                    return !result.ErrorCode.HasValue
                        ? new OperationResult<IEnumerable<CodeScanResult>>(result.CodesResult.SelectMany(r =>
                            r.Text.Select(t => new CodeScanResult
                            {
                                Code = t,
                                Kind = r.Type == "CODE_128" ? CodeKind.Code128 : r.Type == "QR_CODE" ? CodeKind.QRCode : CodeKind.Unknown,
                            })))
                        : new OperationResult<IEnumerable<CodeScanResult>>(Cod.InternalError.InternalServerError) { Reference = json };
                }
                return new OperationResult<IEnumerable<CodeScanResult>>(status) { Reference = json };
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
                return new OperationResult<IEnumerable<OCRScanResult>>(Cod.InternalError.GatewayTimeout);
            }

            OperationResult<string> token = await GetAccessToken();
            if (!token.IsSuccess)
            {
                return new OperationResult<IEnumerable<OCRScanResult>>(token);
            }

            string str = $"access_token={token.Result}&detect_direction=true&probability=true";
            if (tryHarder)
            {
                if (stream.CanSeek)
                {
                    stream.Seek(0, SeekOrigin.Begin);
                }

                byte[] buff = stream.ToByteArray();
                string base64 = WebUtility.UrlEncode(Convert.ToBase64String(buff));
                str += $"&image={base64}";
            }
            else
            {
                str += $"&url={WebUtility.UrlEncode(mediaUri.AbsoluteUri)}";
            }

            try
            {
                using HttpClient httpclient = new(HttpHandler.GetHandler(), false)
                {
#if !DEBUG
                    Timeout = TimeSpan.FromSeconds(8)
#endif
                };
                using StringContent post = new(str, Encoding.UTF8, "application/x-www-form-urlencoded");
                string path = tryHarder ? "rest/2.0/ocr/v1/accurate_basic" : "rest/2.0/ocr/v1/general_basic";
                HttpResponseMessage resp = await httpclient.PostAsync($"{Host}/{path}", post);
                int status = (int)resp.StatusCode;
                string json = await resp.Content.ReadAsStringAsync();
                if (status is >= 200 and < 400)
                {
                    BaiduOCRResponse result = JsonSerializer.DeserializeObject<BaiduOCRResponse>(json, JsonSerializationFormat.UnderstoreCase);
                    return result.WordsResult != null && result.WordsResult.Length > 0
                        ? new OperationResult<IEnumerable<OCRScanResult>>(
                            result.WordsResult.Select(r => new OCRScanResult
                            {
                                Text = r.Words,
                                IsConfident = r.Probability.Variance < 0.019d // REMARK (5he11) 方差不能太大，否则识别不准确
                                 && r.Probability.Min > 0.7d // REMARK (5he11) 最小信心不能太小，否则识别不准确
                            }))
                        : new OperationResult<IEnumerable<OCRScanResult>>(Cod.InternalError.InternalServerError) { Reference = json };
                }
                return new OperationResult<IEnumerable<OCRScanResult>>(status) { Reference = json };
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
            OperationResult<BaiduCompareFaceResponse> rs = await CompareFaceAsync(faceMediaUri.AbsoluteUri, frontCNIDMediaUri.AbsoluteUri);
            return rs.IsSuccess && rs.Result.Result.Score >= minScore
                ? OperationResult.Success
                : new OperationResult(Cod.InternalError.InternalServerError) { Reference = rs };
        }

        public async Task<OperationResult<BaiduCompareFaceResponse>> CompareFaceAsync(string faceUrl, string frontCNIDUrl, int retry = 3)
        {
            if (retry <= 0)
            {
                return new OperationResult<BaiduCompareFaceResponse>(Cod.InternalError.GatewayTimeout);
            }

            OperationResult<string> token = await GetAccessToken();
            if (!token.IsSuccess)
            {
                return new OperationResult<BaiduCompareFaceResponse>(token);
            }

            try
            {
                using HttpClient httpClient = new(HttpHandler.GetHandler(), false);
                string url = $"{Host}/rest/2.0/face/v3/match?access_token={token.Result}";

                List<Dictionary<string, string>> content = new()
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
                string js = JsonSerializer.SerializeObject(content);
                using StringContent post = new(js, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync(url, post);
                int statusCode = (int)response.StatusCode;
                if (statusCode is >= 200 and < 400)
                {
                    js = await response.Content.ReadAsStringAsync();
                    BaiduCompareFaceResponse result = JsonSerializer.DeserializeObject<BaiduCompareFaceResponse>(js, JsonSerializationFormat.UnderstoreCase);
                    return !result.ErrorCode.HasValue || result.ErrorCode == 0
                        ? new OperationResult<BaiduCompareFaceResponse>(result)
                        : new OperationResult<BaiduCompareFaceResponse>(Cod.InternalError.InternalServerError) { Reference = js };
                }
                else
                {
                    return new OperationResult<BaiduCompareFaceResponse>(statusCode) { Reference = js };
                }
            }
            catch
            {

            }
            return await CompareFaceAsync(faceUrl, frontCNIDUrl, --retry);
        }

        private async Task<OperationResult<string>> GetAccessToken(int retry = 0)
        {
            string key = await configuration.Value.GetSettingAsStringAsync(IntegrationKey);
            string token = await cacheStore.Value.GetAsync<string>(key, AccessTokenCacheKey);
            if (!string.IsNullOrWhiteSpace(token))
            {
                return new OperationResult<string>(token);
            }

            string secret = await configuration.Value.GetSettingAsStringAsync(IntegrationSecret);
            try
            {
                using HttpClient httpclient = new(HttpHandler.GetHandler(), false);
                using FormUrlEncodedContent post = new(new Dictionary<string, string>
                {
                    { "grant_type", "client_credentials" },
                    { "client_id", key },
                    { "client_secret", secret },
                });
                HttpResponseMessage resp = await httpclient.PostAsync($"{Host}/oauth/2.0/token", post);
                int status = (int)resp.StatusCode;
                string json = await resp.Content.ReadAsStringAsync();
                if (status is >= 200 and < 400)
                {
                    BaiduAccessTokenResponse result = JsonSerializer.DeserializeObject<BaiduAccessTokenResponse>(json, JsonSerializationFormat.UnderstoreCase);
                    if (!string.IsNullOrWhiteSpace(result.AccessToken))
                    {
                        await cacheStore.Value.SetAsync(key, AccessTokenCacheKey, result.AccessToken, true, DateTimeOffset.UtcNow.Add(result.GetExpiry()));
                        return new OperationResult<string>(result.AccessToken);
                    }
                    else
                    {
                        return new OperationResult<string>(Cod.InternalError.BadGateway) { Reference = result };
                    }
                }
                return new OperationResult<string>(status) { Reference = json };
            }
            catch (TaskCanceledException)
            {
                return retry > 3 ? new OperationResult<string>(Cod.InternalError.GatewayTimeout) : await GetAccessToken(++retry);
            }
        }

        private async Task<OperationResult<BaiduIDScanResponse>> AnalyzeChineseIDAsync(Stream stream, bool isFrontSide, int retry = 3)
        {
            if (retry <= 0)
            {
                return new OperationResult<BaiduIDScanResponse>(Cod.InternalError.GatewayTimeout);
            }

            OperationResult<string> token = await GetAccessToken();
            if (!token.IsSuccess)
            {
                return new OperationResult<BaiduIDScanResponse>(token);
            }

            if (stream.CanSeek)
            {
                stream.Seek(0, SeekOrigin.Begin);
            }

            byte[] buff;
            using (MemoryStream ms = new())
            {
                using Image image = Image.Load(stream);
                double ratio = image.Height / 400d;
                image.Mutate(x => x.Resize((int)(image.Width / ratio), (int)(image.Height / ratio)));
                image.SaveAsJpeg(ms);
                buff = ms.ToByteArray();
            }

            string sideParam = isFrontSide ? "front" : "back";
            string base64 = WebUtility.UrlEncode(Convert.ToBase64String(buff));
            string str = $"access_token={token.Result}&id_card_side={sideParam}&detect_direction=true&detect_risk=true&detect_rectify=true&image={base64}";

            try
            {
                using HttpClient httpclient = new(HttpHandler.GetHandler(), false)
                {
#if !DEBUG
                    Timeout = TimeSpan.FromSeconds(3),
#endif
                };
                using StringContent post = new(str, Encoding.UTF8, "application/x-www-form-urlencoded");
                HttpResponseMessage resp = await httpclient.PostAsync($"{Host}/rest/2.0/ocr/v1/idcard", post);
                int status = (int)resp.StatusCode;
                string json = await resp.Content.ReadAsStringAsync();
                if (status is >= 200 and < 400)
                {
                    BaiduIDScanResponse result = JsonSerializer.DeserializeObject<BaiduIDScanResponse>(json, JsonSerializationFormat.UnderstoreCase);
                    return !result.ErrorCode.HasValue
                        ? new OperationResult<BaiduIDScanResponse>(result)
                        : new OperationResult<BaiduIDScanResponse>(Cod.InternalError.InternalServerError) { Reference = json };
                }
                return new OperationResult<BaiduIDScanResponse>(status) { Reference = json };
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
