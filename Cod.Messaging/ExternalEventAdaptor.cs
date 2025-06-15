namespace Cod.Messaging
{
    internal class ExternalEventAdaptor<TEntity, TEvent> : IExternalEventAdaptor<TEntity, TEvent>
        where TEntity : class
        where TEvent : class, IDomainEvent
    {
        private readonly IEnumerable<IDomainEventHandler<IDomain<TEntity>>> eventHandlers;

        public ExternalEventAdaptor(IEnumerable<IDomainEventHandler<IDomain<TEntity>>> eventHandlers)
        {
            this.eventHandlers = eventHandlers;
        }

        public async Task OnEvent(TEvent e, CancellationToken cancellationToken = default)
        {
            e.Source = DomainEventAudience.External;
            e.Target = DomainEventAudience.Internal;
            await eventHandlers.InvokeAsync(e, cancellationToken);
        }
    }
}
