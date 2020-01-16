namespace Cod.Channel
{
    public interface IViewModel<T>
    {
        IViewModel<T> Initialize(IDomain<T> domain);
    }
}
