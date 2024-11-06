namespace Cod
{
    public abstract class DomainEventHandler<TDomain, TEventArgs> : IDomainEventHandler<TDomain, TEventArgs>
        where TEventArgs : class
    {
        public abstract Task HandleAsync(TEventArgs e, CancellationToken cancellationToken);

        public async Task HandleAsync(object e, CancellationToken cancellationToken)
        {
            if (e is TEventArgs args)
            {
                await HandleAsync(args, cancellationToken);
            }
        }
    }
}
