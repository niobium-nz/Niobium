using System.Text.Json.Serialization;

namespace Niobium.Messaging
{
    public abstract class DomainEvent : IDomainEvent
    {
        protected DomainEvent()
        {
            Source = DomainEventAudience.Internal;
            Target = DomainEventAudience.Internal;
            Occurried = DateTimeOffset.UtcNow;
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
