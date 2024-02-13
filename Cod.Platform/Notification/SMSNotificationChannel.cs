namespace Cod.Platform
{
    public abstract class SMSNotificationChannel : INotificationChannel
    {
        private readonly Lazy<IOpenIDManager> openIDManager;

        public SMSNotificationChannel(Lazy<IOpenIDManager> openIDManager) => this.openIDManager = openIDManager;

        public async Task<OperationResult> SendAsync(
            string brand,
            Guid user,
            NotificationContext context,
            int templateID,
            IReadOnlyDictionary<string, object> parameters,
            int level = 0)
        {
            if (level != (int)OpenIDKind.SMS)
            {
                return OperationResult.NotAllowed;
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
                    return OperationResult.NotAllowed;
                }

                var channels = await this.openIDManager.Value.GetChannelsAsync(user, (int)OpenIDKind.SMS).ToListAsync();
                if (!channels.Any())
                {
                    return OperationResult.NotAllowed;
                }

                // TODO (5he11) 这里取第一个其实是不正确的
                mobile = channels.First().Identity;
            }

            if (String.IsNullOrWhiteSpace(mobile))
            {
                return OperationResult.NotAllowed;
            }

            mobile = mobile.Replace("-", String.Empty).Replace(" ", String.Empty);
            if (String.IsNullOrWhiteSpace(mobile))
            {
                return OperationResult.NotAllowed;
            }

            if (mobile[0] == '+' && !mobile[1..].All(Char.IsDigit))
            {
                return OperationResult.NotAllowed;
            }
            else if (!mobile.All(Char.IsDigit))
            {
                return OperationResult.NotAllowed;
            }

            return await this.SendSMSAsync(brand, mobile, templateID, parameters);
        }

        protected abstract Task<OperationResult> SendSMSAsync(
            string brand,
            string mobile,
            int templateID,
            IReadOnlyDictionary<string, object> parameters);
    }
}
