namespace Niobium
{
    public interface IDomainEventHandler<out TDomain, TEventArgs> : IDomainEventHandler<TDomain>
        where TEventArgs : class
    {
        Task HandleAsync(TEventArgs e, CancellationToken cancellationToken = default);
    }

    public interface IDomainEventHandler<out TDomain>
    {
        Task HandleAsync(object e, CancellationToken cancellationToken = default);
    }
}
