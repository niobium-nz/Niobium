namespace Niobium
{
    public abstract class DomainEventHandler<TDomain, TEventArgs> : IDomainEventHandler<TDomain, TEventArgs>
        where TEventArgs : class, IDomainEvent
    {
        protected virtual DomainEventAudience EventSource => DomainEventAudience.Internal;

        protected virtual DomainEventAudience EventTarget => DomainEventAudience.Internal;

        public async Task HandleAsync(TEventArgs e, CancellationToken cancellationToken = default)
        {
            if (e.Source.HasFlag(EventSource) && e.Target.HasFlag(EventTarget))
            {
                await HandleCoreAsync(e, cancellationToken);
            }
        }

        public abstract Task HandleCoreAsync(TEventArgs e, CancellationToken cancellationToken);

        public async Task HandleAsync(object e, CancellationToken cancellationToken = default)
        {
            if (e is TEventArgs args)
            {
                await HandleAsync(args, cancellationToken);
            }
        }
    }
}
