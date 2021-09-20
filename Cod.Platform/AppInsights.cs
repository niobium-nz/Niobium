using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Cod.Platform
{
    public class AppInsights
    {
        private const string QUERY_TEMPLATE = "{\"query\":\"XXX\",\"workspaceFilters\":{\"regions\":[]}}";
        private readonly Lazy<IConfigurationProvider> configuration;

        public AppInsights(Lazy<IConfigurationProvider> configuration) => this.configuration = configuration;

        public async Task<AppInsightsQueryResult> QueryAsync(string query, DateTimeOffset start, DateTimeOffset end, bool isAzureChina = false)
        {
            var appInsightsAPIAccessApplicationID = await this.configuration.Value.GetSettingAsStringAsync("APPINSIGHTS_APIACCESS_APPLICATION_ID");
            var appInsightsAPIAccessApplicationKey = await this.configuration.Value.GetSettingAsStringAsync("APPINSIGHTS_APIACCESS_APPLICATION_KEY");

            using var httpclient = new HttpClient(HttpHandler.GetHandler(), false);
            var endpoint = isAzureChina ? "https://api.applicationinsights.azure.cn" : "https://api.applicationinsights.io";
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{endpoint}/v1/apps/{appInsightsAPIAccessApplicationID}/query?timespan={start.ToString("o").Replace("+00:00", "Z")}/{end.ToString("o").Replace("+00:00", "Z")}");
            query = query.Replace("\"", "\\\"");
            request.Headers.TryAddWithoutValidation("X-Api-Key", appInsightsAPIAccessApplicationKey);
            request.Content = new StringContent(QUERY_TEMPLATE.Replace("XXX", query, StringComparison.InvariantCulture), Encoding.UTF8, "application/json");
            using var response = await httpclient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var body = await response.Content.ReadAsStringAsync();
            return JsonSerializer.DeserializeObject<AppInsightsQueryResult>(body);
        }
    }
}
