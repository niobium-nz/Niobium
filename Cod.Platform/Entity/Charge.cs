using System;
using System.Collections.Generic;

namespace Cod.Platform
{
    public class Charge
    {
        public TopupTargetKind TopupKind { get; set; }

        public Guid Target { get; set; }

        public PaymentKinds PaymentKind { get; set; }

        public string AppID { get; set; }

        public OpenIDKind OpenIDKind { get; set; }

        public string Device { get; set; }

        public string Order { get; set; }

        public string Account { get; set; }

        public int Amount { get; set; }

        public string Product { get; set; }

        public string IP { get; set; }

        public Dictionary<string, object> Params { get; set; }

        public bool Validate() => this.Params != null
            && this.Params.Count >= 6
            && this.Params.ContainsKey("appId")
            && this.Params.ContainsKey("timeStamp")
            && this.Params.ContainsKey("nonceStr")
            && this.Params.ContainsKey("package")
            && this.Params.ContainsKey("signType")
            && this.Params.ContainsKey("paySign");

        public string ToClientInstructions()
            => this.PaymentKind == PaymentKinds.Wechat ? JsonSerializer.SerializeObject(
                new
                {
                    appId = this.Params["appId"],
                    timeStamp = this.Params["timeStamp"],
                    nonceStr = this.Params["nonceStr"],
                    package = this.Params["package"],
                    signType = this.Params["signType"],
                    paySign = this.Params["paySign"],
                }) : throw new NotSupportedException();
    }
}
