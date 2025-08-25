namespace Niobium.Channel
{
    public interface ICreatableRepository<TDomain, TEntity, TCreateParams> : IRepository<TDomain, TEntity>
        where TDomain : IDomain<TEntity>
        where TEntity : class, new()
        where TCreateParams : class
    {
        Task<OperationResult<TDomain>> CreateAsync(TCreateParams parameters);
    }
}
