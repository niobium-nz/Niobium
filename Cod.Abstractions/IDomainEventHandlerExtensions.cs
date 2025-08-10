namespace Cod
{
    public static class IDomainEventHandlerExtensions
    {
        public static async Task InvokeAsync<TEventArgs, TDomain>(this IEnumerable<IDomainEventHandler<TDomain>> eventHandlers, TEventArgs e, CancellationToken cancellationToken = default)
            where TEventArgs : class
        {
            await eventHandlers.InvokeAsync(() => e, cancellationToken);
        }

        internal static async Task InvokeAsync<TEventArgs, TDomain>(this IEnumerable<IDomainEventHandler<TDomain>> eventHandlers, Func<TEventArgs> getEventArgs, CancellationToken cancellationToken = default)
            where TEventArgs : class
        {
            TEventArgs? args = null;

            foreach (IDomainEventHandler<TDomain> handler in eventHandlers)
            {
                if (handler is IDomainEventHandler<TDomain, TEventArgs> h)
                {
                    args ??= getEventArgs();
                    await h.HandleAsync(args, cancellationToken);
                }
                else if (handler is IDomainEventHandler<TDomain> h2)
                {
                    args ??= getEventArgs();
                    await h2.HandleAsync(args, cancellationToken);
                }
                else if (handler is IDomainEventHandler<IDomain<TDomain>, TEventArgs> h3)
                {
                    args ??= getEventArgs();
                    await h3.HandleAsync(args, cancellationToken);
                }
                else if (handler is IDomainEventHandler<IDomain<TDomain>> h4)
                {
                    args ??= getEventArgs();
                    await h4.HandleAsync(args, cancellationToken);
                }
            }
        }
    }
}
