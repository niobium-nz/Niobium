using Azure.Messaging.ServiceBus;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.Net.Http.Headers;
using Niobium.Identity;

namespace Niobium.Messaging.ServiceBus
{
    internal sealed class ServiceBusQueueBroker<T>(AuthenticationBasedQueueFactory factory, Lazy<IAuthenticator> authenticator) : IMessagingBroker<T> where T : class, IDomainEvent
    {
        public const string MessageContentType = "application/json";

        private static string QueueName
        {
            get
            {
                Type type = typeof(T);
                if (type.IsGenericType)
                {
                    Type[] arguments = type.GetGenericArguments();
                    string name = type.Name.Split('`')[0];
                    return $"{name.ToLowerInvariant()}-{string.Join("-", arguments.Select(arg => arg.Name.ToLowerInvariant()))}";
                }
                else
                {
                    return type.Name.ToLowerInvariant();
                }
            }
        }

        public async Task<MessagingEntry<T>?> DequeueAsync(TimeSpan? maxWaitTime = default, CancellationToken cancellationToken = default)
        {
            ServiceBusReceiver q = await GetReceiverAsync(MessagingPermissions.ProcessMessages, cancellationToken);
            ServiceBusReceivedMessage msg = await q.ReceiveMessageAsync(maxWaitTime, cancellationToken);
            return msg == null || msg.ContentType != MessageContentType
                ? null
                : (MessagingEntry<T>)new ServiceBusMessageEntry<T>(msg, async (m) => await q.CompleteMessageAsync(m))
                {
                    ID = msg.MessageId,
                    Timestamp = msg.EnqueuedTime,
                    Body = msg.Body.ToString(),
                    Type = msg.ApplicationProperties.TryGetValue(HeaderNames.ContentType, out object? type) ? type?.ToString() : null,
                };
        }

        public async Task<IEnumerable<MessagingEntry<T>>> DequeueAsync(int limit, TimeSpan? maxWaitTime = default, CancellationToken cancellationToken = default)
        {
            ServiceBusReceiver q = await GetReceiverAsync(MessagingPermissions.ProcessMessages, cancellationToken);
            IReadOnlyList<ServiceBusReceivedMessage> msgs = await q.ReceiveMessagesAsync(limit, maxWaitTime, cancellationToken);
            List<MessagingEntry<T>> result = [];
            foreach (ServiceBusReceivedMessage? msg in msgs)
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
                    Type = msg.ApplicationProperties.TryGetValue(HeaderNames.ContentType, out object? type) ? type?.ToString() : null,
                });
            }

            return result;
        }

        public async Task EnqueueAsync(IEnumerable<MessagingEntry<T>> messages, CancellationToken cancellationToken = default)
        {
            ServiceBusSender q = await GetSenderAsync(MessagingPermissions.Add, cancellationToken);
            foreach (MessagingEntry<T> message in messages)
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

        public async Task<IEnumerable<MessagingEntry<T>>> PeekAsync(int? limit, CancellationToken cancellationToken = default)
        {
            ServiceBusReceiver q = await GetReceiverAsync(MessagingPermissions.ProcessMessages, cancellationToken);
            IReadOnlyList<ServiceBusReceivedMessage> msgs = await q.PeekMessagesAsync(limit ?? 1000, cancellationToken: cancellationToken);
            if (msgs == null)
            {
                return [];
            }

            List<MessagingEntry<T>> result = [];
            foreach (ServiceBusReceivedMessage? msg in msgs)
            {
                if (msg == null)
                {
                    continue;
                }

                if (msg.ContentType != MessageContentType)
                {
                    throw new ApplicationException(InternalError.BadRequest);
                }

                if (msg.TryParse(out MessagingEntry<T>? entry))
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

        private Task<ServiceBusReceiver> GetReceiverAsync(MessagingPermissions permission, CancellationToken cancellationToken)
        {
            return factory.CreateReceiverAsync([permission], QueueName, cancellationToken);
        }

        private Task<ServiceBusSender> GetSenderAsync(MessagingPermissions permission, CancellationToken cancellationToken)
        {
            return factory.CreateSenderAsync([permission], QueueName, cancellationToken);
        }
    }
}
