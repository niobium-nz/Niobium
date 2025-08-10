using System.Text;

namespace Cod.Platform.Analytics
{
    public class AppInsights(Lazy<IConfigurationProvider> configuration)
    {
        private const string QUERY_TEMPLATE = "{\"query\":\"XXX\",\"workspaceFilters\":{\"regions\":[]}}";

        public async Task<AppInsightsQueryResult> QueryAsync(string query, DateTimeOffset start, DateTimeOffset end, bool isAzureChina = false)
        {
            string? appInsightsAPIAccessApplicationID = await configuration.Value.GetSettingAsStringAsync("APPINSIGHTS_APIACCESS_APPLICATION_ID");
            string? appInsightsAPIAccessApplicationKey = await configuration.Value.GetSettingAsStringAsync("APPINSIGHTS_APIACCESS_APPLICATION_KEY");

            using HttpClient httpclient = new(HttpHandler.GetHandler(), false);
            string endpoint = isAzureChina ? "https://api.applicationinsights.azure.cn" : "https://api.applicationinsights.io";
            using HttpRequestMessage request = new(HttpMethod.Post, $"{endpoint}/v1/apps/{appInsightsAPIAccessApplicationID}/query?timespan={start.ToString("o").Replace("+00:00", "Z")}/{end.ToString("o").Replace("+00:00", "Z")}");
            query = query.Replace("\\", "\\\\").Replace("\"", "\\\""); // REMARK (5he11) json escape
            request.Headers.TryAddWithoutValidation("X-Api-Key", appInsightsAPIAccessApplicationKey);
            request.Content = new StringContent(QUERY_TEMPLATE.Replace("XXX", query, StringComparison.InvariantCulture), Encoding.UTF8, "application/json");
            using HttpResponseMessage response = await httpclient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                AppInsightsQueryResult error = new()
                {
                    Success = false,
                    Error = await response.Content.ReadAsStringAsync()
                };
                return error;
            }

            string body = await response.Content.ReadAsStringAsync();
            AppInsightsQueryResult result = JsonSerializer.DeserializeObject<AppInsightsQueryResult>(body);
            result.Success = true;
            return result;
        }
    }
}
