namespace Cod.Messaging
{
    public interface IExternalEventAdaptor<TEntity, TEvent>
        where TEntity : class
        where TEvent : class
    {
        Task OnEvent(TEvent e, CancellationToken cancellationToken = default);
    }
}
