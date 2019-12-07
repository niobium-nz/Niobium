using Cod.Contract;

namespace Cod.Channel
{
    public interface IDomain : IEntity
    {
        bool Initialized { get; }
    }

    public interface IDomain<T> : IDomain
    {
        T Entity { get; }

        IDomain<T> Initialize(T entity);
    }
}
