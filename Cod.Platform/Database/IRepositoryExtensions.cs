namespace Cod.Platform.Database
{
    public static class IRepositoryExtensions
    {
        public static Task<IEnumerable<T>> CreateAsync<T>(this IRepository<T> repository, T entity, bool replaceIfExist = false, DateTimeOffset? expiry = null, CancellationToken cancellationToken = default)
        {
            return repository.CreateAsync(new[] { entity }, replaceIfExist: replaceIfExist, expiry: expiry, cancellationToken: cancellationToken);
        }

        public static Task<IEnumerable<T>> UpdateAsync<T>(this IRepository<T> repository, T entity, bool preconditionCheck = true, CancellationToken cancellationToken = default)
        {
            return repository.UpdateAsync(new[] { entity }, preconditionCheck: preconditionCheck, cancellationToken: cancellationToken);
        }

        public static Task<IEnumerable<T>> DeleteAsync<T>(this IRepository<T> repository, T entity, bool preconditionCheck = true, bool successIfNotExist = false, CancellationToken cancellationToken = default)
        {
            return repository.DeleteAsync(new[] { entity }, preconditionCheck: preconditionCheck, successIfNotExist: successIfNotExist, cancellationToken: cancellationToken);
        }
    }
}
