using Azure.Messaging.ServiceBus;
using System.Diagnostics.CodeAnalysis;

namespace Cod.Messaging.ServiceBus
{
    public static class ServiceBusReceivedMessageExtensions
    {
        public static bool TryParse<T>(this ServiceBusReceivedMessage message, [NotNullWhen(true)] out T? result) where T : IDomainEvent
        {
            if (message == null)
            {
                result = default;
                return false;
            }
            if (message.ContentType != "application/json")
            {
                result = default;
                return false;
            }
            try
            {
                result = message.Body.ToObjectFromJson<T>();
                if (result == null)
                {
                    return false;
                }

                result.ID = message.MessageId;
                result.Occurried = message.EnqueuedTime;
                result.Source = DomainEventAudience.External;
                result.Target = DomainEventAudience.Internal;
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
