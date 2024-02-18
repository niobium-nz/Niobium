namespace Cod.Platform.OCR.Azure
{
    internal class CognitiveServiceReadAnalyzeResult
    {
        public string Version { get; set; }

        public CognitiveServiceReadResult[] ReadResults { get; set; }
    }
}
