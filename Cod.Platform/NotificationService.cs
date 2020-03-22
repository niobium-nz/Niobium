using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cod.Platform
{
    internal class NotificationService : INotificationService
    {
        private readonly Lazy<INotificationService> service;
        private readonly Lazy<IEnumerable<INotificationChannel>> channels;

        public NotificationService(Lazy<INotificationService> service, Lazy<IEnumerable<INotificationChannel>> channels)
        {
            this.service = service;
            this.channels = channels;
        }

        public async Task<OperationResult> SendAsync(
            string brand,
            OpenIDProvider provider,
            string appID,
            string openID,
            int template,
            IReadOnlyDictionary<string, string> parameters,
            int startLevel = 0,
            int maxLevel = 10)
        {
            var level = startLevel;
            if (level > maxLevel)
            {
                return OperationResult.Create(InternalError.InternalServerError);
            }

            foreach (var channel in channels.Value)
            {
                var result = await channel.SendAsync(brand, provider, appID, openID, template, parameters, level);
                if (result.IsSuccess)
                {
                    return result;
                }
                else if (result.Code == InternalError.NotAllowed)
                {
                    continue;
                }
                else
                {
                    break;
                }
            }
            return await this.service.Value.SendAsync(brand, provider, appID, openID, template, parameters, ++level);
        }
    }
}
