namespace Cod
{
    public class EntityChangedEventArgs<T> : EventArgs, IDomainEvent
        where T : class
    {
        public EntityChangedEventArgs(T oldEntity, T newEntity)
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
