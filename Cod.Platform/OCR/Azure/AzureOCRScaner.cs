using System.Net.Sockets;
using System.Text;

namespace Cod.Platform.OCR.Azure
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
                using HttpClient httpclient = new(HttpHandler.GetHandler(), false)
                {
#if !DEBUG
                    Timeout = TimeSpan.FromSeconds(5),
#endif
                };
                string key = await configuration.Value.GetSettingAsStringAsync(CognitiveKey);
                string endpoint = await configuration.Value.GetSettingAsStringAsync(CognitiveEndpoint);
                httpclient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", key);
                using StringContent post = new(CognitiveRequestBody
                    .Replace("$$$MEDIA_URL$$$", mediaURL), Encoding.UTF8, "application/json");
                HttpResponseMessage resp = await httpclient.PostAsync($"{endpoint}/vision/v3.0/read/analyze", post);
                if (resp.IsSuccessStatusCode)
                {
                    string operationLocation = resp.Headers.GetValues("Operation-Location").Single();
                    string ocrResultJson;
                    int i = 0;
                    do
                    {
                        await Task.Delay(1000);
                        HttpResponseMessage ocrResultResponse = await httpclient.GetAsync(operationLocation);
                        ocrResultJson = await ocrResultResponse.Content.ReadAsStringAsync();
                        ++i;
                    }
                    while (i < 60 && !ocrResultJson.Contains("\"status\":\"succeeded\"", StringComparison.InvariantCultureIgnoreCase));

                    if (i == 60 && !ocrResultJson.Contains("\"status\":\"succeeded\"", StringComparison.InvariantCultureIgnoreCase))
                    {
                        return new OperationResult<IEnumerable<OCRScanResult>>(Cod.InternalError.BadGateway) { Reference = operationLocation };
                    }

                    CognitiveServiceResult ocr = JsonSerializer.DeserializeObject<CognitiveServiceResult>(ocrResultJson);
                    List<OCRScanResult> result = new(ocr.AnalyzeResult.ReadResults.SelectMany(r =>
                            r.Lines.SelectMany(l =>
                                l.Words.Select(w => new OCRScanResult
                                {
                                    Text = w.Text.EndsWith("7A", StringComparison.InvariantCultureIgnoreCase) ? w.Text[0..^2] : w.Text, // REMARK (5he11) Azure has issue with recognition of "湖" and it'd end up with "7A"
                                    IsConfident = w.Confidence > 0.63d,
                                }))));
                    result.AddRange(ocr.AnalyzeResult.ReadResults.SelectMany(r =>
                            r.Lines.Select(l => new OCRScanResult
                            {
                                Text = l.Text.EndsWith("7A", StringComparison.InvariantCultureIgnoreCase) ? l.Text[0..^2] : l.Text,
                                IsConfident = l.Words.Max(w => w.Confidence) > 0.63d,
                            })));
                    return new OperationResult<IEnumerable<OCRScanResult>>(result);
                }

                int status = (int)resp.StatusCode;
                string errormsg = await resp.Content.ReadAsStringAsync();
                return new OperationResult<IEnumerable<OCRScanResult>>(status) { Reference = errormsg };
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
