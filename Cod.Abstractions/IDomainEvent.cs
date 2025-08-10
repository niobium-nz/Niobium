namespace Cod
{
    public interface IDomainEvent
    {
        string ID { get; set; }

        DateTimeOffset Occurried { get; set; }

        DomainEventAudience Source { get; set; }

        DomainEventAudience Target { get; set; }
    }
}
