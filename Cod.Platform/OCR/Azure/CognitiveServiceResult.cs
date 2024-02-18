namespace Cod.Platform.OCR.Azure
{
    internal class CognitiveServiceResult
    {
        public string Status { get; set; }

        public DateTimeOffset CreatedDateTime { get; set; }

        public DateTimeOffset LastUpdatedDateTime { get; set; }

        public CognitiveServiceReadAnalyzeResult AnalyzeResult { get; set; }
    }
}
