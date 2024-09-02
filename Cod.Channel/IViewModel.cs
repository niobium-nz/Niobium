using System.Threading.Tasks;

namespace Cod.Channel
{
    public interface IViewModel<TDomain, TEntity>
        where TDomain : IDomain<TEntity>
        where TEntity : class, new()
    {
        string PartitionKey { get; }

        string RowKey { get; }

        Task<string> GetHashAsync();

        IUIRefreshable Parent { get; }

        Task<IViewModel<TDomain, TEntity>> InitializeAsync(TDomain domain, IUIRefreshable parent = null, bool force = false);
    }
}
