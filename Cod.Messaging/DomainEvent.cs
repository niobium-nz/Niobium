using System.Text.Json.Serialization;

namespace Cod.Messaging
{
    public abstract class DomainEvent : IDomainEvent
    {
        protected DomainEvent()
        {
            Source = Cod.DomainEventAudience.Internal;
            Target = Cod.DomainEventAudience.Internal;
        }

        public string ID { get; set; } = Guid.NewGuid().ToString();

        [JsonIgnore]
        public DateTimeOffset Occurried { get; set; }

        [JsonIgnore]
        public DomainEventAudience Source { get; set; }

        [JsonIgnore]
        public DomainEventAudience Target { get; set; }
    }
}
