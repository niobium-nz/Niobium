using Azure.Messaging.ServiceBus;
using Microsoft.Net.Http.Headers;
using System.Diagnostics.CodeAnalysis;

namespace Niobium.Messaging.ServiceBus
{
    public static class ServiceBusReceivedMessageExtensions
    {
        public static bool TryParse<T>(this ServiceBusReceivedMessage message, [NotNullWhen(true)] out MessagingEntry<T>? result)
            where T : class, IDomainEvent
        {
            if (message == null)
            {
                result = default;
                return false;
            }

            if (message.ContentType != ServiceBusQueueBroker<T>.MessageContentType)
            {
                result = default;
                return false;
            }

            try
            {
                Type? type = null;
                if (message.ApplicationProperties.TryGetValue(HeaderNames.ContentType, out object? v))
                {
                    try
                    {
                        type = Type.GetType(v.ToString()!);
                    }
                    catch
                    {
                    }
                }

                string json = message.Body.ToString();
                result = MessagingEntry.Parse<T>(json, type);

                if (result == null)
                {
                    return false;
                }

                result.ID = message.MessageId;
                result.Timestamp = message.EnqueuedTime;

                if (result.Value != null)
                {
                    result.Value.ID = message.MessageId;
                    result.Value.Occurried = message.EnqueuedTime;
                }

                return true;
            }
            catch
            {
                result = default;
                return false;
            }
        }
    }
}
