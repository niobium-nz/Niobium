using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cod.Platform
{
    public abstract class SMSNotificationChannel : INotificationChannel
    {
        private readonly Lazy<IOpenIDManager> openIDManager;

        public SMSNotificationChannel(Lazy<IOpenIDManager> openIDManager)
        {
            this.openIDManager = openIDManager;
        }

        public async Task<OperationResult> SendAsync(
            string brand,
            Guid user,
            NotificationContext context,
            int template,
            IReadOnlyDictionary<string, object> parameters,
            int level = 0)
        {
            if (level != (int)OpenIDKind.SMS)
            {
                return OperationResult.Create(InternalError.NotAllowed);
            }

            string mobile = null;
            if (parameters.ContainsKey(NotificationParameters.PreferredMobile)
                && parameters[NotificationParameters.PreferredMobile] is string s)
            {
                mobile = s;
            }

            if (mobile == null)
            {
                if (user == Guid.Empty)
                {
                    return OperationResult.Create(InternalError.NotAllowed);
                }

                var channels = await this.openIDManager.Value.GetChannelsAsync(user, (int)OpenIDKind.SMS);
                if (!channels.Any())
                {
                    return OperationResult.Create(InternalError.NotAllowed);
                }

                // TODO (5he11) 这里取第一个其实是不正确的
                mobile = channels.First().Identity;
            }

            if (string.IsNullOrWhiteSpace(mobile))
            {
                return OperationResult.Create(InternalError.NotAllowed);
            }

            mobile = mobile.Replace("-", string.Empty).Replace(" ", string.Empty);
            if (string.IsNullOrWhiteSpace(mobile))
            {
                return OperationResult.Create(InternalError.NotAllowed);
            }

            if (mobile[0] == '+' && !mobile.Substring(1, mobile.Length - 1).All(Char.IsDigit))
            {
                return OperationResult.Create(InternalError.NotAllowed);
            }
            else if (!mobile.All(Char.IsDigit))
            {
                return OperationResult.Create(InternalError.NotAllowed);
            }

            return await this.SendSMSAsync(brand, mobile, template, parameters);
        }

        protected abstract Task<OperationResult> SendSMSAsync(
            string brand,
            string mobile,
            int template,
            IReadOnlyDictionary<string, object> parameters);
    }
}
