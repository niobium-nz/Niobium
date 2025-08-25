namespace Niobium.Messaging
{
    internal sealed class ExternalEventAdaptor<TEntity, TEvent>(IEnumerable<IDomainEventHandler<IDomain<TEntity>>> eventHandlers) : IExternalEventAdaptor<TEntity, TEvent>
        where TEntity : class
        where TEvent : class, IDomainEvent
    {
        public async Task OnEvent(TEvent e, CancellationToken cancellationToken = default)
        {
            e.Source = DomainEventAudience.External;
            e.Target = DomainEventAudience.Internal;
            await eventHandlers.InvokeAsync(e, cancellationToken);
        }
    }
}
