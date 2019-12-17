using System;
using Microsoft.Extensions.Logging;

namespace Cod.Platform.Model
{
    public abstract class ChargeNotification
    {
        public string AppID { get; protected set; }

        public ChargeType ChargeType { get; protected set; }

        public int Amount { get; protected set; }

        public string Order { get; protected set; }

        public string Account { get; protected set; }

        public string Message { get; protected set; }

        public string InternalSignature { get; protected set; }

        public DateTimeOffset Paid { get; protected set; }

        public string Reference { get; protected set; }

        public ChargeNotification(string content) => this.ParseContent(content);

        public abstract bool Success();

        public virtual bool Validate(string platformSecret, string merchantSecret)
        {
            if (!this.Success())
            {
                this.LogError($"微信回调通知失败. 错误消息: {this.Message}");
                return false;
            }

            var toSign = $"{this.AppID}|{this.Account}|{this.Amount}";
            var signature = SHA.SHA256Hash(toSign, platformSecret, 127);
            if (signature != this.InternalSignature)
            {
                this.LogError($"验证内部签名失败. 微信返回内部签名:{this.InternalSignature} 自签:{signature}");
                return false;
            }
            return true;
        }

        protected abstract void ParseContent(string content);

        protected void LogError(string log)
        {
            var logger = Logger.Instance;
            if (logger != null)
            {
                logger.LogError(log);
            }
        }
    }
}
