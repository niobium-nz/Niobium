using System;
using System.Collections.Generic;

namespace Cod.Platform.Charges
{
    public class WechatChargeNotification : ChargeNotification
    {
        public string WechatOrder { get; private set; }

        public string AppID { get; private set; }

        public string MerchantID { get; private set; }

        public string NonceString { get; private set; }

        public string WechatSignature { get; private set; }

        public ChargeType ChargeType { get; private set; }

        public string ResultCode { get; private set; }

        public string ResultMessage { get; private set; }

        public int Amount { get; private set; }

        public string Order { get; private set; }

        public string Account { get; private set; }

        public string InternalSignature { get; private set; }

        public DateTimeOffset PaidAt { get; private set; }

        public Dictionary<string, string> WechatDatas { get; private set; }

        public WechatChargeNotification(string content) : base(content)
        {
        }

        protected override void ParseContent(string content)
        {
            var param = WechatHelper.FromXML(content);
            this.WechatOrder = param["transaction_id"];
            this.AppID = param["appid"];
            this.MerchantID = param["mch_id"];
            this.NonceString = param["nonce_str"];
            this.WechatSignature = param["sign"];
            this.ResultCode = param["result_code"];
            if (param.ContainsKey("return_msg"))
            {
                this.ResultMessage = param["return_msg"];
            }
            this.Amount = int.Parse(param["total_fee"]);
            this.Order = param["out_trade_no"];
            this.Account = param["openid"];
            this.InternalSignature = param["attach"];
            var time = param["time_end"];
            this.PaidAt = new DateTimeOffset(int.Parse(time.Substring(0, 4)),
                int.Parse(time.Substring(4, 2)),
                int.Parse(time.Substring(6, 2)),
                int.Parse(time.Substring(8, 2)),
                int.Parse(time.Substring(10, 2)),
                int.Parse(time.Substring(12, 2)),
                TimeSpan.FromHours(8));

            if (param["trade_type"].ToUpperInvariant() == "JSAPI")
            {
                this.ChargeType = ChargeType.JSAPI;
            }
            else
            {
                throw new NotSupportedException();
            }
            this.WechatDatas = param;
        }
    }
}
