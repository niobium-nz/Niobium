using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Cod.Platform
{
    public static class IRepositoryExtensions
    {

        public static Task<TableQueryResult<T>> GetAsync<T>(this IRepository<T> repository)
            => repository.GetAsync(-1);

        public static Task<TableQueryResult<T>> GetAsync<T>(this IRepository<T> repository, string partitionKey)
            => repository.GetAsync(partitionKey, -1);

        public static Task CreateAsync<T>(this IRepository<T> repository, IEnumerable<T> entities, ILogger logger)
            => repository.CreateAsync(entities, false, logger);

        public static Task CreateAsync<T>(this IRepository<T> repository, T entity, bool replaceIfExist, ILogger logger)
            => repository.CreateAsync(new[] { entity }, replaceIfExist, logger);

        public static Task CreateAsync<T>(this IRepository<T> repository, T entity, ILogger logger)
            => repository.CreateAsync(entity, false, logger);

        public static Task UpdateAsync<T>(this IRepository<T> repository, T entity)
            => repository.UpdateAsync(new[] { entity });

        public static Task DeleteAsync<T>(this IRepository<T> repository, T entity)
            => repository.DeleteAsync(new[] { entity });
    }
}
