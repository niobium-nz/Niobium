namespace Cod.Channel
{
    public interface ICRUDRepository<TDomain, TEntity>
       : IDeletableRepository<TDomain, TEntity>
       where TDomain : IDomain<TEntity>
       where TEntity : class, new()
    {
    }

    public interface ICRUDRepository<TDomain, TEntity, TCreateParams>
       : ICreatableRepository<TDomain, TEntity, TCreateParams>,
       IDeletableRepository<TDomain, TEntity>
       where TDomain : IDomain<TEntity>
       where TCreateParams : class
       where TEntity : class, new()
    {
    }

    public interface ICRUDRepository<TDomain, TEntity, TCreateParams, TUpdateParams>
        : ICreatableRepository<TDomain, TEntity, TCreateParams>,
        IUpdatableRepository<TDomain, TEntity, TUpdateParams>,
        IDeletableRepository<TDomain, TEntity>
        where TDomain : IDomain<TEntity>
        where TCreateParams : class
        where TEntity : class, new()
    {
    }
}
