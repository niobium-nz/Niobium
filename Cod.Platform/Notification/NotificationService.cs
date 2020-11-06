using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cod.Platform
{
    internal class NotificationService : INotificationService
    {
        private readonly Lazy<IEnumerable<INotificationChannel>> channels;

        public NotificationService(Lazy<IEnumerable<INotificationChannel>> channels) => this.channels = channels;

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
                return new OperationResult<int>(InternalError.InternalServerError);
            }

            foreach (var channel in this.channels.Value)
            {
                var result = await channel.SendAsync(brand, user, context, template, parameters, level);
                await this.PostSendAsync(result, brand, user, context, template, parameters, level);
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
                    if (level + 1 > maxLevel)
                    {
                        return new OperationResult<int>(result);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return await this.SendAsync(brand, user, context, template, parameters, ++level);
        }

        protected virtual Task PostSendAsync(OperationResult result, string brand, Guid user, NotificationContext context, int template, IReadOnlyDictionary<string, object> parameters, int level)
            => Task.CompletedTask;
    }
}
