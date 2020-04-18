using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cod.Platform
{
    public abstract class PushNotificationChannel : INotificationChannel
    {
        private readonly Lazy<IOpenIDManager> openIDManager;

        public PushNotificationChannel(Lazy<IOpenIDManager> openIDManager)
        {
            this.openIDManager = openIDManager;
        }

        public virtual async Task<OperationResult> SendAsync(
            string brand,
            string account,
            NotificationContext context,
            int template,
            IReadOnlyDictionary<string, object> parameters,
            int level = 0)
        {
            if (level == (int)OpenIDKind.Email
                || level == (int)OpenIDKind.PhoneCall
                || level == (int)OpenIDKind.SMS)
            {
                return OperationResult.Create(InternalError.NotAllowed);
            }

            IEnumerable<NotificationContext> targets;
            if (context != null)
            {
                targets = new List<NotificationContext> { context };
            }
            else
            {
                var openid = await openIDManager.Value.GetChannelsAsync(account, level);
                targets = openid.Select(i => new NotificationContext(level, i.GetApp(), i.GetAccount()));
            }

            if (!targets.Any())
            {
                return OperationResult.Create(InternalError.NotAllowed);
            }

            return await this.SendPushAsync(brand, targets, template, parameters);
        }

        protected abstract Task<OperationResult> SendPushAsync(
            string brand,
            IEnumerable<NotificationContext> targets,
            int template,
            IReadOnlyDictionary<string, object> parameters);
    }
}
