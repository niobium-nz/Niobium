namespace Cod.Platform.Analytics
{
    public class AppInsightsQueryResultTable
    {
        public string Name { get; set; }

        public AppInsightsQueryResultTableColumn[] Columns { get; set; }

        public string[][] Rows { get; set; }
    }
}
