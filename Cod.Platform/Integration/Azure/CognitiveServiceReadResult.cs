namespace Cod.Platform
{
    internal class CognitiveServiceReadResult
    {
        public int Page { get; set; }

        public string Language { get; set; }

        public double Angle { get; set; }

        public double Width { get; set; }

        public double Height { get; set; }

        public string Unit { get; set; }

        public CognitiveServiceReadLine[] Lines { get; set; }
    }
}
