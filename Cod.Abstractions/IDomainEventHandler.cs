namespace Cod
{
    public interface IDomainEventHandler<TSender, TEventArgs> : IEventHandler<TSender>
        where TSender : IDomain
        where TEventArgs : new()
    {
        Task HandleAsync(TSender sender, TEventArgs e);
    }
}
