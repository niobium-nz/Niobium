using Azure.Messaging.ServiceBus;

namespace Cod.Messaging.ServiceBus
{
    internal class ServiceBusMessageEntry<T>(ServiceBusReceivedMessage message, Func<ServiceBusReceivedMessage, Task> completeMessage) : MessagingEntry<T>
    {
        private ServiceBusReceivedMessage? message = message;
        private Func<ServiceBusReceivedMessage, Task>? completeMessage = completeMessage;

        protected override async ValueTask DisposeAsync(bool disposing)
        {
            if (disposing && completeMessage != null && message != null)
            {
                await completeMessage(message);
                message = null;
                completeMessage = null;
            }
        }
    }
}
