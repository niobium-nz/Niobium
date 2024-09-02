using Cod.Platform.Tenant;

namespace Cod.Platform.Finance.WechatPay
{
    public class WechatPayer
    {
        public string AppID { get; set; }

        public string OpenID { get; set; }

        public string MerchantID { get; set; }

        public OpenIDKind OpenIDKind { get; set; }
    }
}
