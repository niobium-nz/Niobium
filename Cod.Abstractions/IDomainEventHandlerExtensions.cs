namespace Cod
{
    public static class IDomainEventHandlerExtensions
    {
        public async static Task InvokeAsync<TEventArgs, TDomain>(this IEnumerable<IDomainEventHandler<TDomain>> eventHandlers, TEventArgs e, CancellationToken cancellationToken = default)
            where TEventArgs : class
        {
            foreach (var handler in eventHandlers)
            {
                if (handler is IDomainEventHandler<TDomain, TEventArgs> h)
                {
                    await h.HandleAsync(e, cancellationToken);
                }
                else if (handler is IDomainEventHandler<TDomain> h2)
                {
                    await h2.HandleAsync(e, cancellationToken);
                }
                else if (handler is IDomainEventHandler<IDomain<TDomain>, TEventArgs> h3)
                {
                    await h3.HandleAsync(e, cancellationToken);
                }
                else if (handler is IDomainEventHandler<IDomain<TDomain>> h4)
                {
                    await h4.HandleAsync(e, cancellationToken);
                }
            }
        }
    }
}
