using System.Text.Json.Serialization;

namespace Cod.Messaging
{
    public abstract class DomainEvent : IDomainEvent
    {
        [JsonIgnore]
        public string ID { get; set; } = string.Empty;

        [JsonIgnore]
        public DateTimeOffset Occurried { get; set; }

        [JsonIgnore]
        public DomainEventAudience Source { get; set; }

        [JsonIgnore]
        public DomainEventAudience Target { get; set; }
    }
}
