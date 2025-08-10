namespace Cod.Channel
{
    public interface IDeletableRepository<TDomain, TEntity> : IRepository<TDomain, TEntity>
        where TDomain : IDomain<TEntity>
        where TEntity : class, new()
    {
        Task<OperationResult> DeleteAsync(StorageKey key);
    }
}
