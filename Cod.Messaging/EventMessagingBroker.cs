namespace Cod.Messaging
{
    public class EventMessagingBroker<TDomain, TEntity, TEvent> : DomainEventHandler<TDomain, TEvent>
        where TDomain : class, IDomain<TEntity>
        where TEntity : class
        where TEvent : class, IDomainEvent
    {
        private readonly Lazy<IMessagingBroker<TEvent>> broker;

        protected override DomainEventAudience EventTarget => DomainEventAudience.External;

        public EventMessagingBroker(Lazy<IMessagingBroker<TEvent>> broker)
        {
            this.broker = broker;
        }

        public async override Task HandleCoreAsync(TEvent e, CancellationToken cancellationToken = default)
        {
            if (e.Source == DomainEventAudience.Internal && e.Target.HasFlag(DomainEventAudience.External))
            {
                await broker.Value.EnqueueAsync(new MessagingEntry<TEvent> { Value = e, }, cancellationToken);
            }
        }
    }
}
