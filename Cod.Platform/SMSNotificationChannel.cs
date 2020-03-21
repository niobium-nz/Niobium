using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cod.Platform
{
    public abstract class SMSNotificationChannel : INotificationChannel
    {
        public const int Level = 1;

        public async Task<OperationResult> SendAsync(
            string brand,
            OpenIDProvider provider,
            string appID,
            string openID,
            int template,
            IReadOnlyDictionary<string, string> parameters,
            int level = 0)
        {
            if (level != Level)
            {
                return OperationResult.Create(InternalError.NotAllowed);
            }

            if (!parameters.ContainsKey(NotificationParameters.Mobile))
            {
                return OperationResult.Create(InternalError.BadRequest);
            }

            return await this.SendSMSAsync(parameters[NotificationParameters.Mobile],
                brand, provider, appID, openID, template, parameters);
        }

        protected abstract Task<OperationResult> SendSMSAsync(
            string mobile,
            string brand,
            OpenIDProvider provider,
            string appID,
            string openID,
            int template,
            IReadOnlyDictionary<string, string> parameters);
    }
}
