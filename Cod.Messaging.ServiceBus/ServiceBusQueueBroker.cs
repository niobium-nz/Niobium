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

        public virtual async Task<MessagingEntry<T>?> DequeueAsync(TimeSpan? maxWaitTime = default, CancellationToken cancellationToken = default)
        {
            var q = await GetReceiverAsync(MessagingPermissions.ProcessMessages, cancellationToken);
            var msg = await q.ReceiveMessageAsync(maxWaitTime, cancellationToken);
            if (msg == null || msg.ContentType != MessageContentType)
            {
                return null;
            }
            
            var value = msg.Body.ToObjectFromJson<T>(SerializationOptions);
            if (value == null)
            {
                return null;
            }
            
            return new ServiceBusMessageEntry<T>(msg, async (m) => await q.CompleteMessageAsync(m))
            {
                ID = msg.MessageId,
                Timestamp = msg.EnqueuedTime,
                Value = value,
            };
        }

        public virtual async Task<IEnumerable<MessagingEntry<T>>> DequeueAsync(int limit, TimeSpan? maxWaitTime = default, CancellationToken cancellationToken = default)
        {
            var q = await GetReceiverAsync(MessagingPermissions.ProcessMessages, cancellationToken);
            var msgs = await q.ReceiveMessagesAsync(limit, maxWaitTime, cancellationToken);
            var result = new List<MessagingEntry<T>>();
            foreach (var msg in msgs)
            {
                if (msg.ContentType != MessageContentType)
                {
                    throw new ApplicationException(InternalError.BadRequest);
                }

                var value = msg.Body.ToObjectFromJson<T>(SerializationOptions) ?? throw new ApplicationException(InternalError.BadRequest);
                result.Add(new ServiceBusMessageEntry<T>(msg, async (m) => await q.CompleteMessageAsync(m))
                {
                    ID = msg.MessageId,
                    Timestamp = msg.EnqueuedTime,
                    Value = value,
                });
            }

            return result;
        }

        public virtual async Task EnqueueAsync(IEnumerable<MessagingEntry<T>> messages, CancellationToken cancellationToken = default)
        {
            var q = await GetSenderAsync(MessagingPermissions.Add, cancellationToken);
            foreach (var message in messages)
            {
                var json = Serialize(message.Value);
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

        public virtual async Task<IEnumerable<MessagingEntry<T>>> PeekAsync(int? limit, CancellationToken cancellationToken = default)
        {
            var q = await GetReceiverAsync(MessagingPermissions.ProcessMessages, cancellationToken);
            var msgs = await q.PeekMessagesAsync(limit ?? 1000, cancellationToken: cancellationToken);
            var result = new List<MessagingEntry<T>>();
            foreach (var msg in msgs)
            {
                if (msg.ContentType != MessageContentType)
                {
                    throw new ApplicationException(InternalError.BadRequest);
                }

                var value = msg.Body.ToObjectFromJson<T>(SerializationOptions) ?? throw new ApplicationException(InternalError.BadRequest);
                result.Add(new MessagingEntry<T>
                {
                    ID = msg.MessageId,
                    Timestamp = msg.EnqueuedTime,
                    Value = value,
                });
            }

            return result;
        }

        protected virtual Task<ServiceBusReceiver> GetReceiverAsync(MessagingPermissions permission, CancellationToken cancellationToken)
            => factory.CreateReceiverAsync([permission], QueueName, cancellationToken);

        protected virtual Task<ServiceBusSender> GetSenderAsync(MessagingPermissions permission, CancellationToken cancellationToken)
            => factory.CreateSenderAsync([permission], QueueName, cancellationToken);

        private static string Serialize(object obj)
            => System.Text.Json.JsonSerializer.Serialize(obj, SerializationOptions)!;
    }
}
