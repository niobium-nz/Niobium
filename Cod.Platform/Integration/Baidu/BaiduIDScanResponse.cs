using System.Collections.Generic;

namespace Cod.Platform
{
    internal class BaiduIDScanResponse : BaiduIntegrationResponse
    {
        public long LogID { get; set; }

        public int Direction { get; set; }

        public int WordsResultNum { get; set; }

        public string EditTool { get; set; }

        public string ImageStatus { get; set; }

        public string RiskType { get; set; }

        public int IDCardNumberType { get; set; }

        public Dictionary<string, BaiduOCRResult> WordsResult { get; set; }
    }
}
