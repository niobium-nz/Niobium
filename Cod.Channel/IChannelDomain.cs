using Cod.Contract;

namespace Cod.Channel
{
    public interface IChannelDomain<T> : IDomain<T>
    {
        T Entity { get; }
    }
}
