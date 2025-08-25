namespace Niobium.Platform.Notification
{
    internal sealed class NotificationService(Lazy<IEnumerable<INotificationChannel>> channels) : INotificationService
    {
        private static readonly IReadOnlyDictionary<string, object> EmptyParameters = new Dictionary<string, object>();

        public async Task<OperationResult<int>> SendAsync(
            string brand,
            Guid user,
            NotificationContext context,
            int template,
            IReadOnlyDictionary<string, object> parameters,
            int startLevel = 1,
            int maxLevel = 100)
        {
            int level = startLevel;
            if (level > maxLevel)
            {
                return new OperationResult<int>(Niobium.InternalError.InternalServerError);
            }

            parameters ??= EmptyParameters;

            foreach (INotificationChannel channel in channels.Value)
            {
                OperationResult result = await channel.SendAsync(brand, user, context, template, parameters, level);
                await PostSendAsync(result, brand, user, context, template, parameters, level);
                if (result.IsSuccess)
                {
                    return new OperationResult<int>(result) { Result = level };
                }
                else if (result.Code == Niobium.InternalError.NotAllowed)
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

            return await SendAsync(brand, user, context, template, parameters, ++level);
        }

        private static Task PostSendAsync(OperationResult result, string brand, Guid user, NotificationContext context, int template, IReadOnlyDictionary<string, object> parameters, int level)
        {
            return Task.CompletedTask;
        }
    }
}
