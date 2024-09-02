namespace Cod
{
    public interface IEventHandler<out T>
    {
        Task HandleAsync(object sender, object e);
    }
}
