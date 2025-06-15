namespace Cod
{
    public interface IDomainEvent
    {
        public string ID { get; set; }

        public DateTimeOffset Occurried { get; set; }

        public DomainEventAudience Source { get; set; }

        public DomainEventAudience Target { get; set; }
    }
}
