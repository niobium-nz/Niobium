namespace Cod
{
    public class EntityChangedEvent<T> : EventArgs, IDomainEvent
        where T : class
    {
        public EntityChangedEvent(T oldEntity, T newEntity)
        {
            Source = DomainEventAudience.Internal;
            Target = DomainEventAudience.Internal;
            OldEntity = oldEntity;
            NewEntity = newEntity;
        }

        public string ID { get; set; } = string.Empty;

        public DateTimeOffset Occurried { get; set; }

        public DomainEventAudience Source { get; set; }

        public DomainEventAudience Target { get; set; }

        public T OldEntity { get; }

        public T NewEntity { get; }
    }
}
