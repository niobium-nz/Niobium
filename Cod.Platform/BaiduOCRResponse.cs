namespace Cod.Platform
{
    public class BaiduOCRResponse : BaiduIntegrationResponse
    {
        public long LogID { get; set; }

        public int Direction { get; set; }

        public int WordsResultNum { get; set; }

        public BaiduOCRResult[] WordsResult { get; set; }
    }
}
