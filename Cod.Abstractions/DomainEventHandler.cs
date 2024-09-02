namespace Cod
{
    public abstract class DomainEventHandler<TDomain, TEventArgs> : IDomainEventHandler<TDomain, TEventArgs>
        where TDomain : IDomain
        where TEventArgs : new()
    {
        public abstract Task HandleAsync(TDomain sender, TEventArgs e);

        public async Task HandleAsync(object sender, object e)
        {
            if (sender is TDomain s && e is TEventArgs args)
            {
                await HandleAsync(s, args);
            }
        }
    }
}
