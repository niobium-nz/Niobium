namespace Niobium.Messaging.ServiceBus
{
    public class ServiceBusOptions
    {
        public string? FullyQualifiedNamespace { get; set; }

        public bool EnableInteractiveIdentity { get; set; }

        public bool UseWebSocket { get; set; }

        public int MaxRetries { get; set; } = 5;

        public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(5);

        public TimeSpan? ConnectionIdleTimeout { get; set; }

        public Dictionary<string, string>? Keys { get; set; }
    }
}
