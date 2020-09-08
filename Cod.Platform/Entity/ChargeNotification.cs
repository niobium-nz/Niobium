using System;
using Microsoft.Extensions.Logging;

namespace Cod.Platform
{
    public abstract class ChargeNotification
    {
        public string AppID { get; protected set; }

        public ChargeType ChargeType { get; protected set; }

        public int Amount { get; protected set; }

        public string Order { get; protected set; }

        public string Account { get; protected set; }

        public string Message { get; protected set; }

        public string Attach { get; protected set; }

        public DateTimeOffset Paid { get; protected set; }

        public string Reference { get; protected set; }

        public ChargeNotification(string content) => this.ParseContent(content);

        public abstract bool Success();

        public Guid GetTarget()
        {
            return Guid.Parse(this.Attach.Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries)[1]);
        }

        public TopupTargetKind GetKind() {
            return (TopupTargetKind)int.Parse(this.Attach.Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries)[0]);
        }

        public virtual bool Validate(string merchantSecret)
        {
            if (!this.Success())
            {
                this.LogError($"微信回调通知失败. 错误消息: {this.Message}");
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
