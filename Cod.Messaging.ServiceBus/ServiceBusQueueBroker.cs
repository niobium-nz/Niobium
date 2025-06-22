using Azure.Messaging.ServiceBus;
using Cod.Identity;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.Net.Http.Headers;

namespace Cod.Messaging.ServiceBus
{
    internal class ServiceBusQueueBroker<T>(AuthenticationBasedQueueFactory factory, Lazy<IAuthenticator> authenticator) : IMessagingBroker<T> where T : class, IDomainEvent
    {
        public const string MessageContentType = "application/json";

        protected virtual string QueueName 
        { 
            get
            {
                var type = typeof(T);
                if (type.IsGenericType)
                {
                    var arguments = type.GetGenericArguments();
                    var name = type.Name.Split('`')[0];
                    return $"{name.ToLowerInvariant()}-{string.Join("-", arguments.Select(arg => arg.Name.ToLowerInvariant()))}";
                }
                else
                {
                    return type.Name.ToLowerInvariant();
                }
            }
        }

        public virtual async Task<MessagingEntry<T>?> DequeueAsync(TimeSpan? maxWaitTime = default, CancellationToken cancellationToken = default)
        {
            var q = await GetReceiverAsync(MessagingPermissions.ProcessMessages, cancellationToken);
            var msg = await q.ReceiveMessageAsync(maxWaitTime, cancellationToken);
            if (msg == null || msg.ContentType != MessageContentType)
            {
                return null;
            }

            return new ServiceBusMessageEntry<T>(msg, async (m) => await q.CompleteMessageAsync(m))
            {
                ID = msg.MessageId,
                Timestamp = msg.EnqueuedTime,
                Body = msg.Body.ToString(),
                Type = msg.ApplicationProperties.TryGetValue(HeaderNames.ContentType, out var type) ? type?.ToString() : null,
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

                result.Add(new ServiceBusMessageEntry<T>(msg, async (m) => await q.CompleteMessageAsync(m))
                {
                    ID = msg.MessageId,
                    Timestamp = msg.EnqueuedTime,
                    Body = msg.Body.ToString(),
                    Type = msg.ApplicationProperties.TryGetValue(HeaderNames.ContentType, out var type) ? type?.ToString() : null,
                });
            }

            return result;
        }

        public virtual async Task EnqueueAsync(IEnumerable<MessagingEntry<T>> messages, CancellationToken cancellationToken = default)
        {
            var q = await GetSenderAsync(MessagingPermissions.Add, cancellationToken);
            foreach (var message in messages)
            {
                if (string.IsNullOrWhiteSpace(message.ID))
                {
                    message.ID = Guid.NewGuid().ToString();
                }

                ServiceBusMessage sbmessage = new(message.Body)
                {
                    MessageId = message.ID,
                    ContentType = MessageContentType,
                };

                if (message.Type != null)
                {
                    sbmessage.ApplicationProperties.Add(HeaderNames.ContentType, message.Type);
                }

                JsonWebToken? accessToken = null;
                try
                {
                    accessToken = authenticator.Value.AccessToken;
                }
                catch
                {
                    // ignore access token in case if authenticator is not available
                }

                if (accessToken != null)
                {
                    sbmessage.ApplicationProperties.Add(HeaderNames.Authorization, $"{AuthenticationScheme.BearerLoginScheme} {accessToken.EncodedToken}");
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
            if (msgs == null)
            {
                return [];
            }

            var result = new List<MessagingEntry<T>>();
            foreach (var msg in msgs)
            {
                if (msg == null)
                {
                    continue;
                }

                if (msg.ContentType != MessageContentType)
                {
                    throw new ApplicationException(InternalError.BadRequest);
                }

                if (msg.TryParse<T>(out var entry))
                {
                    result.Add(entry);
                }
                else
                {
                    throw new ApplicationException(InternalError.InternalServerError, $"Error parsing service bus message into {typeof(T).FullName}: {msg.Body}");
                }
            }

            return result;
        }

        protected virtual Task<ServiceBusReceiver> GetReceiverAsync(MessagingPermissions permission, CancellationToken cancellationToken)
            => factory.CreateReceiverAsync([permission], QueueName, cancellationToken);

        protected virtual Task<ServiceBusSender> GetSenderAsync(MessagingPermissions permission, CancellationToken cancellationToken)
            => factory.CreateSenderAsync([permission], QueueName, cancellationToken);
    }
}
