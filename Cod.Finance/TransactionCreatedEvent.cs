using System.Text.Json.Serialization;

namespace Cod.Finance
{
    public class TransactionCreatedEvent(Transaction newTransaction) : IDomainEvent
    {
        [JsonIgnore]
        public string ID { get; set; } = string.Empty;

        [JsonIgnore]
        public DateTimeOffset Occurried { get; set; }

        [JsonIgnore]
        public DomainEventAudience Source { get; set; }

        [JsonIgnore]
        public DomainEventAudience Target { get; set; }

        public Transaction Transaction { get; } = newTransaction ?? throw new ArgumentNullException(nameof(newTransaction));
    }
}
