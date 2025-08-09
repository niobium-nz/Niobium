namespace Cod.Platform.Analytics
{
    public class AppInsightsQueryResultTable
    {
        public required string Name { get; set; }

        public required AppInsightsQueryResultTableColumn[] Columns { get; set; }

        public required string[][] Rows { get; set; }
    }
}
