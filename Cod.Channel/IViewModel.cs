using System.Threading.Tasks;

namespace Cod.Channel
{
    public interface IViewModel<TDomain, TEntity>
        where TDomain : IChannelDomain<TEntity>
        where TEntity : IEntity
    {
        string PartitionKey { get; }

        string RowKey { get; }

        string ETag { get; }

        Task<IViewModel<TDomain, TEntity>> InitializeAsync(TDomain domain);
    }
}
