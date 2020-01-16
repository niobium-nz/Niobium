namespace Cod
{
    public interface IDomain : IEntity
    {
        bool Initialized { get; }
    }

    public interface IDomain<T> : IDomain
    {
        IDomain<T> Initialize(T entity);
    }
}
