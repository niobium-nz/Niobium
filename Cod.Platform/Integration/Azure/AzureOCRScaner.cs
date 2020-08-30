using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Cod.Platform
{
    public class AzureOCRScaner
    {
        private const string CognitiveKey = "COGNITIVE_KEY";
        private const string CognitiveEndpoint = "COGNITIVE_ENDPOINT";
        private const string CognitiveRequestBody = "{\"url\":\"$$$MEDIA_URL$$$\"}";
        private readonly Lazy<IConfigurationProvider> configuration;

        public AzureOCRScaner(Lazy<IConfigurationProvider> configuration)
        {
            this.configuration = configuration;
        }

        public async Task<OperationResult<IEnumerable<OCRScanResult>>> PerformOCRAsync(string mediaURL, int retry = 0)
        {
            try
            {
                using (var httpclient = new HttpClient(HttpHandler.GetHandler(), false)
                {
#if !DEBUG
                    Timeout = TimeSpan.FromSeconds(5),
#endif
                })
                {
                    var key = await this.configuration.Value.GetSettingAsStringAsync(CognitiveKey);
                    var endpoint = await this.configuration.Value.GetSettingAsStringAsync(CognitiveEndpoint);
                    httpclient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", key);
                    using (var post = new StringContent(CognitiveRequestBody
                        .Replace("$$$MEDIA_URL$$$", mediaURL), Encoding.UTF8, "application/json"))
                    {
                        var resp = await httpclient.PostAsync($"{endpoint}/vision/v3.0/read/analyze", post);
                        if (resp.IsSuccessStatusCode)
                        {
                            var operationLocation = resp.Headers.GetValues("Operation-Location").Single();
                            string ocrResultJson;
                            int i = 0;
                            do
                            {
                                await Task.Delay(1000);
                                var ocrResultResponse = await httpclient.GetAsync(operationLocation);
                                ocrResultJson = await ocrResultResponse.Content.ReadAsStringAsync();
                                ++i;
                            }
                            while (i < 60 && ocrResultJson.IndexOf("\"status\":\"succeeded\"") == -1);

                            if (i == 60 && ocrResultJson.IndexOf("\"status\":\"succeeded\"") == -1)
                            {
                                return OperationResult<IEnumerable<OCRScanResult>>.Create(InternalError.GatewayTimeout, operationLocation);
                            }

                            var ocr = JsonConvert.DeserializeObject<CognitiveServiceResult>(ocrResultJson);
                            var result = new List<OCRScanResult>(ocr.AnalyzeResult.ReadResults.SelectMany(r =>
                                    r.Lines.SelectMany(l =>
                                        l.Words.Select(w => new OCRScanResult
                                        {
                                            Text = w.Text.EndsWith("7A") ? w.Text.Substring(0, w.Text.Length - 2) : w.Text, // REMARK (5he11) Azure has issue with recognition of "湖" and it'd end up with "7A"
                                            IsConfident = w.Confidence > 0.8d,
                                        }))));
                            result.AddRange(ocr.AnalyzeResult.ReadResults.SelectMany(r =>
                                    r.Lines.Select(l => new OCRScanResult
                                        {
                                            Text = l.Text.EndsWith("7A") ? l.Text.Substring(0, l.Text.Length - 2) : l.Text,
                                            IsConfident = true,
                                        })));
                            return OperationResult<IEnumerable<OCRScanResult>>.Create(result);
                        }

                        var status = (int)resp.StatusCode;
                        var errormsg = await resp.Content.ReadAsStringAsync();
                        return OperationResult<IEnumerable<OCRScanResult>>.Create(status, errormsg);
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

            return await PerformOCRAsync(mediaURL, ++retry);
        }
    }
}
