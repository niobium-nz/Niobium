namespace Cod
{
    public interface IDomainEventHandler<out TDomain, TEventArgs> : IDomainEventHandler<TDomain>
        where TEventArgs : class
    {
        Task HandleAsync(TEventArgs e, CancellationToken? cancellationToken);
    }

    public interface IDomainEventHandler<out TDomain>
    {
        Task HandleAsync(object e, CancellationToken? cancellationToken);
    }
}
