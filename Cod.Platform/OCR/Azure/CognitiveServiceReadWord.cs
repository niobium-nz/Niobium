namespace Cod.Platform.OCR.Azure
{
    internal class CognitiveServiceReadWord
    {
        public int[] BoundingBox { get; set; }

        public string Text { get; set; }

        public double Confidence { get; set; }
    }
}
