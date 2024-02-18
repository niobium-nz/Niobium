namespace Cod.Platform.Analytics
{
    public class AppInsightsQueryResult
    {
        public bool Success { get; set; }

        public string Error { get; set; }

        public AppInsightsQueryResultTable[] Tables { get; set; }
    }
}
