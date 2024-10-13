using Azure.Messaging.ServiceBus;
using Cod.Identity;
using Microsoft.Net.Http.Headers;
using System.Text.Json;

namespace Cod.Messaging.ServiceBus
{
    internal class ServiceBusQueueBroker<T>(AuthenticationBasedQueueFactory factory, IAuthenticator authenticator) : IMessagingBroker<T> where T : class, new()
    {
        private const string MessageContentType = "application/json";
        private static readonly JsonSerializerOptions SerializationOptions = new(JsonSerializerDefaults.Web);
        protected virtual string QueueName { get => typeof(T).Name.ToLowerInvariant(); }

        public virtual Task<MessagingEntry<T>> DequeueAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public virtual Task<IEnumerable<MessagingEntry<T>>> DequeueAsync(int limit, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public virtual async Task EnqueueAsync(IEnumerable<MessagingEntry<T>> messages, CancellationToken cancellationToken = default)
        {
            var q = await GetSenderAsync(cancellationToken);
            foreach (var message in messages)
            {
                var json = Serialize(message);
                ServiceBusMessage sbmessage = new(json)
                {
                    MessageId = message.ID,
                    ContentType = MessageContentType,
                };

                if (authenticator.AccessToken != null)
                {
                    sbmessage.ApplicationProperties.Add(HeaderNames.Authorization, $"{AuthenticationScheme.BearerLoginScheme} {authenticator.AccessToken.EncodedToken}");
                }

                if (message.Schedule.HasValue)
                {
                    sbmessage.ScheduledEnqueueTime = message.Schedule.Value;
                }

                await q.SendMessageAsync(sbmessage, cancellationToken);
            }
        }

        public virtual Task<IEnumerable<MessagingEntry<T>>> PeekAsync(int? limit, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }


        protected virtual Task<ServiceBusSender> GetSenderAsync(CancellationToken cancellationToken)
            => factory.CreateQueueAsync(QueueName, cancellationToken);



        private static string Serialize(object obj)
            => System.Text.Json.JsonSerializer.Serialize(obj, SerializationOptions)!;
    }
}
