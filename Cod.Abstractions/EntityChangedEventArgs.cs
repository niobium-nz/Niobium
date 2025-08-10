namespace Cod
{
    public class EntityChangedEventArgs<T>(EntityChangeType changeType, T entity) : EventArgs, IDomainEvent
        where T : class
    {
        public string ID { get; set; } = string.Empty;

        public DateTimeOffset Occurried { get; set; }

        public DomainEventAudience Source { get; set; } = DomainEventAudience.Internal;

        public DomainEventAudience Target { get; set; } = DomainEventAudience.Internal;

        public T Entity { get; } = entity;

        public EntityChangeType ChangeType { get; } = changeType;
    }
}
