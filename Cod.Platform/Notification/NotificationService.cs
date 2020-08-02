using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cod.Platform
{
    internal class NotificationService : INotificationService
    {
        private readonly Lazy<IEnumerable<INotificationChannel>> channels;

        public NotificationService(Lazy<IEnumerable<INotificationChannel>> channels)
        {
            this.channels = channels;
        }

        public async Task<OperationResult<int>> SendAsync(
            string brand,
            Guid user,
            NotificationContext context,
            int template,
            IReadOnlyDictionary<string, object> parameters,
            int startLevel = 1,
            int maxLevel = 100)
        {
            var level = startLevel;
            if (level > maxLevel)
            {
                return OperationResult<int>.Create(InternalError.InternalServerError, null);
            }

            foreach (var channel in channels.Value)
            {
                var result = await channel.SendAsync(brand, user, context, template, parameters, level);
                if (result.IsSuccess)
                {
                    return new OperationResult<int>(result) { Result = level };
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
            return await this.SendAsync(brand, user, context, template, parameters, ++level);
        }
    }
}
