namespace Cod.Channel
{
    public interface IChannelDomain<T> : IDomain<T> where T : IEntity
    {
        T Entity { get; }
    }
}
