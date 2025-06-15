namespace Cod
{
    public abstract class DomainEventHandler<TDomain, TEventArgs> : IDomainEventHandler<TDomain, TEventArgs>
        where TEventArgs : class, IDomainEvent
    {
        public async Task HandleAsync(TEventArgs e, CancellationToken cancellationToken = default)
        {
            if (e.Target.HasFlag(DomainEventAudience.Internal))
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
