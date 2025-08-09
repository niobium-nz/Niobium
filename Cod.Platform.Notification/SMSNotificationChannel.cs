namespace Cod.Platform.Notification
{
    public abstract class SMSNotificationChannel(Lazy<INofiticationChannelRepository> openIDManager) : INotificationChannel
    {
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

            string? mobile = null;
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

                List<OpenID> channels = await openIDManager.Value.GetChannelsAsync(user, (int)OpenIDKind.SMS).ToListAsync();
                if (channels.Count == 0)
                {
                    return OperationResult.NotAllowed;
                }

                // TODO (5he11) 这里取第一个其实是不正确的
                mobile = channels.First().Identity;
            }

            if (string.IsNullOrWhiteSpace(mobile))
            {
                return OperationResult.NotAllowed;
            }

            mobile = mobile.Replace("-", string.Empty).Replace(" ", string.Empty);
            if (string.IsNullOrWhiteSpace(mobile))
            {
                return OperationResult.NotAllowed;
            }

            if (mobile[0] == '+' && !mobile[1..].All(char.IsDigit))
            {
                return OperationResult.NotAllowed;
            }
            else if (!mobile.All(char.IsDigit))
            {
                return OperationResult.NotAllowed;
            }

            return await SendSMSAsync(brand, mobile, templateID, parameters);
        }

        protected abstract Task<OperationResult> SendSMSAsync(
            string brand,
            string mobile,
            int templateID,
            IReadOnlyDictionary<string, object> parameters);
    }
}
