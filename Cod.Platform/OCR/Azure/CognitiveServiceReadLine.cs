namespace Cod.Platform.OCR.Azure
{
    internal class CognitiveServiceReadLine
    {
        public int[] BoundingBox { get; set; }

        public string Text { get; set; }

        public CognitiveServiceReadWord[] Words { get; set; }
    }
}
