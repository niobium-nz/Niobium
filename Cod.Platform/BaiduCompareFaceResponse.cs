using System;
using System.Collections.Generic;
using System.Text;

namespace Cod.Platform
{
    public class BaiduCompareFaceResponse : BaiduIntegrationResponse
    {
        public long LogID { get; set; }
        public BaiduCompareFaceResult Result { get; set; }
    }
}
