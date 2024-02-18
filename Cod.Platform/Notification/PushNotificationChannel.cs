using Cod.Platform.Identities;

namespace Cod.Platform.Notification
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
            Guid user,
            NotificationContext context,
            int templateID,
            IReadOnlyDictionary<string, object> parameters,
            int level = 0)
        {
            if (level is ((int)OpenIDKind.Email)
                or ((int)OpenIDKind.PhoneCall)
                or ((int)OpenIDKind.SMS))
            {
                return OperationResult.NotAllowed;
            }

            IEnumerable<NotificationContext> targets = Enumerable.Empty<NotificationContext>();
            if (context != null)
            {
                targets = new List<NotificationContext> { context };
            }
            else if (user != Guid.Empty)
            {
                // TODO (5he11) 根据 context 决定 app 下边的查询可以更高效
                // TODO (5he11) 传递 IAsyncEnumerable 到下游
                List<Authentication.OpenID> openid = await openIDManager.Value.GetChannelsAsync(user, level).ToListAsync();
                targets = openid.Select(i => new NotificationContext(level, i.GetApp(), i.GetUser(), i.Identity));
            }

            return !targets.Any() ? OperationResult.NotAllowed : await SendPushAsync(brand, targets, templateID, parameters);
        }

        protected abstract Task<OperationResult> SendPushAsync(
            string brand,
            IEnumerable<NotificationContext> targets,
            int templateID,
            IReadOnlyDictionary<string, object> parameters);
    }
}
