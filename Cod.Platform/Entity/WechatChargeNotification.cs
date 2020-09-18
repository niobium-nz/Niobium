using System;
using System.Collections.Generic;
using System.Linq;

namespace Cod.Platform
{
    public class WechatChargeNotification : ChargeNotification
    {
        public string MerchantID { get; private set; }

        public string NonceString { get; private set; }

        public string WechatSignature { get; private set; }

        public string ResultCode { get; private set; }

        public Dictionary<string, string> Params { get; private set; }

        public WechatChargeNotification(string content) : base(content)
        {
        }

        protected override void ParseContent(string content)
        {
            var param = WechatIntegration.FromXML(content);
            this.Reference = param["transaction_id"];
            this.AppID = param["appid"];
            this.MerchantID = param["mch_id"];
            this.NonceString = param["nonce_str"];
            this.WechatSignature = param["sign"];
            this.ResultCode = param["result_code"];
            if (param.ContainsKey("return_msg"))
            {
                this.Message = param["return_msg"];
            }
            this.Amount = Int32.Parse(param["total_fee"]);
            this.Order = param["out_trade_no"];
            this.Account = param["openid"];
            this.Attach = param["attach"];
            var time = param["time_end"];
            this.Paid = new DateTimeOffset(Int32.Parse(time.Substring(0, 4)),
                Int32.Parse(time.Substring(4, 2)),
                Int32.Parse(time.Substring(6, 2)),
                Int32.Parse(time.Substring(8, 2)),
                Int32.Parse(time.Substring(10, 2)),
                Int32.Parse(time.Substring(12, 2)),
                TimeSpan.FromHours(8));

            if (param["trade_type"].ToUpperInvariant() == "JSAPI")
            {
                this.PaymentKind = PaymentKinds.Wechat;
            }
            else
            {
                throw new NotSupportedException();
            }
            this.Params = param;
        }

        public override bool Success() => this.ResultCode != null && this.ResultCode.ToUpperInvariant() == "SUCCESS";

        public override bool Validate(string merchantSecret)
        {
            var result = base.Validate(merchantSecret);
            if (!result)
            {
                return false;
            }

            var signature = WechatIntegration.MD5Sign(this.Params.Where(k => k.Key != "sign").OrderBy(k => k.Key), merchantSecret);
            if (signature != this.WechatSignature)
            {
                this.LogError($"验证微信签名失败. 微信返回签名:{this.WechatSignature} 自签:{signature}");
                return false;
            }

            return true;
        }
    }
}
