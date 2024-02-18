using Cod.Platform.Tenants.Wechat;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace Cod.Platform.Finance.WechatPay
{
    public class WechatChargeNotification
    {
        public WechatChargeNotification(string content)
        {
            ParseContent(content);
        }

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
        {
            return offset <= 0 ? $"{(int)kind}|{target.Trim()}" : $"{(int)kind}|{target.Trim()}|{offset}";
        }

        public bool Success()
        {
            return ResultCode != null && ResultCode.ToUpperInvariant() == "SUCCESS";
        }

        public bool Validate(string merchantSecret)
        {
            if (!Success())
            {
                LogError($"微信回调通知失败. 错误消息: {Message}");
                return false;
            }

            string signature = WechatIntegration.MD5Sign(Params.Where(k => k.Key != "sign").OrderBy(k => k.Key), merchantSecret);
            if (signature != WechatSignature)
            {
                LogError($"验证微信签名失败. 微信返回签名:{WechatSignature} 自签:{signature}");
                return false;
            }

            return true;
        }

        public string GetTarget()
        {
            return Attach.Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries)[1];
        }

        public ChargeTargetKind GetKind()
        {
            return (ChargeTargetKind)int.Parse(Attach.Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries)[0], CultureInfo.InvariantCulture);
        }

        private void ParseContent(string content)
        {
            Dictionary<string, string> param = WechatIntegration.FromXML(content);
            Reference = param["transaction_id"];
            AppID = param["appid"];
            MerchantID = param["mch_id"];
            NonceString = param["nonce_str"];
            WechatSignature = param["sign"];
            ResultCode = param["result_code"];
            if (param.ContainsKey("return_msg"))
            {
                Message = param["return_msg"];
            }
            Amount = int.Parse(param["total_fee"], CultureInfo.InvariantCulture);
            Order = param["out_trade_no"];
            Account = param["openid"];
            Device = param["device_info"];
            Attach = param["attach"];
            string time = param["time_end"];
            Paid = new DateTimeOffset(int.Parse(time[..4], CultureInfo.InvariantCulture),
                int.Parse(time.Substring(4, 2), CultureInfo.InvariantCulture),
                int.Parse(time.Substring(6, 2), CultureInfo.InvariantCulture),
                int.Parse(time.Substring(8, 2), CultureInfo.InvariantCulture),
                int.Parse(time.Substring(10, 2), CultureInfo.InvariantCulture),
                int.Parse(time.Substring(12, 2), CultureInfo.InvariantCulture),
                TimeSpan.FromHours(8));

            PaymentKind = param["trade_type"].ToUpperInvariant() == "JSAPI" ? PaymentChannels.Wechat : throw new NotSupportedException();
            Params = param;
        }

        private static void LogError(string log)
        {
            ILogger logger = Logger.Instance;
            logger?.LogError(log);
        }
    }
}
