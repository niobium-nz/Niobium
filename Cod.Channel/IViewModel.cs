namespace Cod.Channel
{
    public interface IViewModel<TDomain, TEntity>
        where TDomain : IChannelDomain<TEntity>
    {
        IViewModel<TDomain, TEntity> Initialize(TDomain domain);
    }
}
