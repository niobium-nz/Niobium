using System.Text.Json.Serialization;

namespace Niobium.Finance
{
    public class TransactionCreatedEvent(Transaction newTransaction) : IDomainEvent
    {
        [JsonIgnore]
        public string ID { get; set; } = string.Empty;

        [JsonIgnore]
        public DateTimeOffset Occurried { get; set; }

        [JsonIgnore]
        public DomainEventAudience Source { get; set; } = DomainEventAudience.Internal;

        [JsonIgnore]
        public DomainEventAudience Target { get; set; } = DomainEventAudience.Internal;

        public Transaction Transaction { get; } = newTransaction ?? throw new ArgumentNullException(nameof(newTransaction));
    }
}
