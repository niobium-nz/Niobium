namespace Cod.Platform
{
    public static class IRepositoryExtensions
    {
        public static Task CreateAsync<T>(this IRepository<T> repository, IEnumerable<T> entities)
            => repository.CreateAsync(entities, false);

        public static Task CreateAsync<T>(this IRepository<T> repository, T entity, bool replaceIfExist)
            => repository.CreateAsync(new[] { entity }, replaceIfExist);

        public static Task CreateAsync<T>(this IRepository<T> repository, T entity)
            => repository.CreateAsync(entity, false);

        public static Task UpdateAsync<T>(this IRepository<T> repository, T entity)
            => repository.UpdateAsync(new[] { entity });

        public static Task DeleteAsync<T>(this IRepository<T> repository, T entity)
            => repository.DeleteAsync(new[] { entity });
    }
}
