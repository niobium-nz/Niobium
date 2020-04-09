namespace Cod.Channel
{
    public interface ICRUDRepository<TDomain, TEntity, TCreateParams>
        : ICreatableRepository<TDomain, TEntity, TCreateParams>,
        IUpdatableRepository<TDomain, TEntity>,
        IDeletableRepository<TDomain, TEntity>
        where TDomain : IChannelDomain<TEntity>
        where TCreateParams : class
    {
    }
}
