namespace Niobium.Channel
{
    public interface IUpdatableRepository<TDomain, TEntity, TUpdateParams> : IRepository<TDomain, TEntity>
        where TDomain : IDomain<TEntity>
        where TEntity : class, new()
    {
        Task<OperationResult<TDomain>> UpdateAsync(TUpdateParams parameter);
    }
}
