namespace Cod.Platform.OCR.Baidu
{
    public class BaiduCodeScanResponse : BaiduIntegrationResponse
    {
        public long LogID { get; set; }

        public int CodesResultNum { get; set; }

        public BaiduCodeScan[] CodesResult { get; set; }
    }
}
