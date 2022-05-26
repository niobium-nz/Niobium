using System.Globalization;
using Microsoft.Extensions.Logging;

namespace Cod.Platform
{
    public class WechatChargeNotification
    {
        public WechatChargeNotification(string content) => this.ParseContent(content);

        public string AppID { get; protected set; }

        public PaymentChannels PaymentKind { get; protected set; }

        public int Amount { get; protected set; }

        public string Order { get; protected set; }

        public string Account { get; protected set; }

        public string Message { get; protected set; }

        public string Attach { get; protected set; }

        public DateTimeOffset Paid { get; protected set; }

        public string Reference { get; protected set; }

        public string Device { get; protected set; }

        public string MerchantID { get; private set; }

        public string NonceString { get; private set; }

        public string WechatSignature { get; private set; }

        public string ResultCode { get; private set; }

        public Dictionary<string, string> Params { get; private set; }

        public static string BuildAttach(ChargeTargetKind kind, string target, int offset = 0)
            => offset <= 0 ? $"{(int)kind}|{target.Trim()}" : $"{(int)kind}|{target.Trim()}|{offset}";

        public bool Success() => this.ResultCode != null && this.ResultCode.ToUpperInvariant() == "SUCCESS";

        public bool Validate(string merchantSecret)
        {
            if (!this.Success())
            {
                LogError($"微信回调通知失败. 错误消息: {this.Message}");
                return false;
            }

            var signature = WechatIntegration.MD5Sign(this.Params.Where(k => k.Key != "sign").OrderBy(k => k.Key), merchantSecret);
            if (signature != this.WechatSignature)
            {
                LogError($"验证微信签名失败. 微信返回签名:{this.WechatSignature} 自签:{signature}");
                return false;
            }

            return true;
        }

        public string GetTarget()
            => this.Attach.Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries)[1];

        public ChargeTargetKind GetKind()
            => (ChargeTargetKind)Int32.Parse(this.Attach.Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries)[0], CultureInfo.InvariantCulture);

        private void ParseContent(string content)
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
            this.Amount = Int32.Parse(param["total_fee"], CultureInfo.InvariantCulture);
            this.Order = param["out_trade_no"];
            this.Account = param["openid"];
            this.Device = param["device_info"];
            this.Attach = param["attach"];
            var time = param["time_end"];
            this.Paid = new DateTimeOffset(Int32.Parse(time[..4], CultureInfo.InvariantCulture),
                Int32.Parse(time.Substring(4, 2), CultureInfo.InvariantCulture),
                Int32.Parse(time.Substring(6, 2), CultureInfo.InvariantCulture),
                Int32.Parse(time.Substring(8, 2), CultureInfo.InvariantCulture),
                Int32.Parse(time.Substring(10, 2), CultureInfo.InvariantCulture),
                Int32.Parse(time.Substring(12, 2), CultureInfo.InvariantCulture),
                TimeSpan.FromHours(8));

            if (param["trade_type"].ToUpperInvariant() == "JSAPI")
            {
                this.PaymentKind = PaymentChannels.Wechat;
            }
            else
            {
                throw new NotSupportedException();
            }
            this.Params = param;
        }

        private static void LogError(string log)
        {
            var logger = Logger.Instance;
            if (logger != null)
            {
                logger.LogError(log);
            }
        }
    }
}
