namespace Cod
{
    public static class IRepositoryExtensions
    {
        public static async Task<T> CreateAsync<T>(this IRepository<T> repository, T entity, bool replaceIfExist = false, DateTimeOffset? expiry = null, CancellationToken cancellationToken = default)
        {
            IEnumerable<T> result = await repository.CreateAsync(new[] { entity }, replaceIfExist: replaceIfExist, expiry: expiry, cancellationToken: cancellationToken);
            return result.SingleOrDefault();
        }

        public static async Task<T> UpdateAsync<T>(this IRepository<T> repository, T entity, bool preconditionCheck = true, CancellationToken cancellationToken = default)
        {
            IEnumerable<T> result = await repository.UpdateAsync(new[] { entity }, preconditionCheck: preconditionCheck, cancellationToken: cancellationToken);
            return result.SingleOrDefault();
        }

        public static async Task DeleteAsync<T>(this IRepository<T> repository, T entity, bool preconditionCheck = true, bool successIfNotExist = true, CancellationToken cancellationToken = default)
        {
            await repository.DeleteAsync(new[] { entity }, preconditionCheck: preconditionCheck, successIfNotExist: successIfNotExist, cancellationToken: cancellationToken);
        }

        public static async Task DeleteAsync<T>(this IRepository<T> repository, string partitionKey, string rowKey, bool successIfNotExist = true, CancellationToken cancellationToken = default)
        {
            T entity = await repository.RetrieveAsync(partitionKey, rowKey, cancellationToken: cancellationToken);
            if (entity != null)
            {
                await repository.DeleteAsync(new[] { entity }, preconditionCheck: false, successIfNotExist: false, cancellationToken: cancellationToken);
            }

            if (entity == null && !successIfNotExist)
            {
                throw new HttpRequestException($"Entity[PartitionKey={partitionKey},RowKey={rowKey}] does not exist.", inner: null /* TODO Pass parameter statusCode: HttpStatusCode.NotFound after upgrading .NET */);
            }
        }

        public static async Task DeleteAsync<T>(this IRepository<T> repository, string partitionKey, CancellationToken cancellationToken = default)
        {
            IAsyncEnumerable<T> entities = repository.GetAsync(partitionKey, cancellationToken: cancellationToken);
            List<T> itemsToDelete = new();
            await foreach (T entity in entities)
            {
                itemsToDelete.Add(entity);
            }

            if (itemsToDelete.Any())
            {
                await repository.DeleteAsync(itemsToDelete, preconditionCheck: false, successIfNotExist: false, cancellationToken: cancellationToken);
            }
        }
    }
}
