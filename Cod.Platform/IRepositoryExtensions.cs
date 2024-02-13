namespace Cod.Platform
{
    public static class IRepositoryExtensions
    {
        public static Task CreateAsync<T>(this IRepository<T> repository, T entity, bool replaceIfExist = false, DateTimeOffset? expiry = null, CancellationToken cancellationToken = default)
            => repository.CreateAsync(new[] { entity }, replaceIfExist: replaceIfExist, expiry: expiry, cancellationToken: cancellationToken);

        public static Task UpdateAsync<T>(this IRepository<T> repository, T entity, bool preconditionCheck = true, CancellationToken cancellationToken = default)
            => repository.UpdateAsync(new[] { entity }, preconditionCheck: preconditionCheck, cancellationToken: cancellationToken);

        public static Task DeleteAsync<T>(this IRepository<T> repository, T entity, bool preconditionCheck = true, bool successIfNotExist = false, CancellationToken cancellationToken = default)
            => repository.DeleteAsync(new[] { entity }, preconditionCheck: preconditionCheck, successIfNotExist: successIfNotExist, cancellationToken: cancellationToken);
    }
}
