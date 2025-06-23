namespace Cod
{
    public class EntityChangedEvent<T> : EventArgs, IDomainEvent
        where T : class
    {
        public EntityChangedEvent(EntityChangeType changeType, T entity)
        {
            Source = DomainEventAudience.Internal;
            Target = DomainEventAudience.Internal;
            ChangeType = changeType;
            Entity = entity;
        }

        public string ID { get; set; } = string.Empty;

        public DateTimeOffset Occurried { get; set; }

        public DomainEventAudience Source { get; set; }

        public DomainEventAudience Target { get; set; }

        public T Entity { get; }

        public EntityChangeType ChangeType { get; }
    }
}
