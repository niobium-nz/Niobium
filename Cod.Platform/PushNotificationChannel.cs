using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cod.Platform.Model;

namespace Cod.Platform
{
    public abstract class PushNotificationChannel : INotificationChannel
    {
        private readonly Lazy<IRepository<OpenID>> repository;

        public PushNotificationChannel(Lazy<IRepository<OpenID>> repository)
        {
            this.repository = repository;
        }

        public async Task<OperationResult> SendAsync(
            string brand,
            string account,
            NotificationContext context,
            int template,
            IReadOnlyDictionary<string, string> parameters,
            int level = 0)
        {
            if (level != NotificationLevels.Push)
            {
                return OperationResult.Create(InternalError.NotAllowed);
            }

            var targets = new List<NotificationContext>();
            if (context.Provider != OpenIDProvider.Nest)
            {
                targets.Add(context);
            }
            else
            {
                for (int i = 0; i < 10; i++)
                {
                    var openID = await repository.Value.GetAsync(
                        OpenID.BuildPartitionKey(this.ProviderSupport, i, context.UserID),
                        OpenID.BuildRowKey(context.UserID));
                    if (openID == null)
                    {
                        break;
                    }
                    else
                    {
                        targets.Add(new NotificationContext(openID.GetProvider(), openID.AppID, openID.UserID));
                    }
                }
            }

            if (targets.Count == 0)
            {
                return OperationResult.Create(InternalError.NotAllowed);
            }

            return await this.SendPushAsync(brand, targets, template, parameters);
        }

        protected abstract OpenIDProvider ProviderSupport { get; }

        protected abstract Task<OperationResult> SendPushAsync(
            string brand,
            IReadOnlyCollection<NotificationContext> targets,
            int template,
            IReadOnlyDictionary<string, string> parameters);
    }
}
