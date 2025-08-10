namespace Cod.Messaging
{
    public class EventMessagingBroker<TDomain, TEntity, TEvent>(Lazy<IMessagingBroker<TEvent>> broker) : DomainEventHandler<TDomain, TEvent>
        where TDomain : class, IDomain<TEntity>
        where TEntity : class
        where TEvent : class, IDomainEvent
    {
        protected override DomainEventAudience EventTarget => DomainEventAudience.External;

        public override async Task HandleCoreAsync(TEvent e, CancellationToken cancellationToken = default)
        {
            if (e.Source == DomainEventAudience.Internal && e.Target.HasFlag(DomainEventAudience.External))
            {
                await broker.Value.EnqueueAsync(new MessagingEntry<TEvent> { Value = e, }, cancellationToken);
            }
        }
    }
}
