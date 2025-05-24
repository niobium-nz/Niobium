namespace Cod
{
    public abstract class DomainEventHandler<TDomain, TEventArgs> : IDomainEventHandler<TDomain, TEventArgs>
        where TEventArgs : class
    {
        public abstract Task HandleAsync(TEventArgs e, CancellationToken? cancellationToken = null);

        public async Task HandleAsync(object e, CancellationToken? cancellationToken = null)
        {
            if (e is TEventArgs args)
            {
                await HandleAsync(args, cancellationToken);
            }
        }
    }
}
