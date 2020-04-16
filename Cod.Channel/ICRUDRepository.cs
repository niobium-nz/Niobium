namespace Cod.Channel
{
    public interface ICRUDRepository<TDomain, TEntity>
       : IDeletableRepository<TDomain, TEntity>
       where TDomain : IChannelDomain<TEntity>
    {
    }

    public interface ICRUDRepository<TDomain, TEntity, TCreateParams>
       : ICreatableRepository<TDomain, TEntity, TCreateParams>,
       IDeletableRepository<TDomain, TEntity>
       where TDomain : IChannelDomain<TEntity>
       where TCreateParams : class
    {
    }

    public interface ICRUDRepository<TDomain, TEntity, TCreateParams, TUpdateParams>
        : ICreatableRepository<TDomain, TEntity, TCreateParams>,
        IUpdatableRepository<TDomain, TEntity, TUpdateParams>,
        IDeletableRepository<TDomain, TEntity>
        where TDomain : IChannelDomain<TEntity>
        where TCreateParams : class
    {
    }
}
