using System.Threading.Tasks;

namespace Cod.Platform
{
    public interface IPlatformDomain<T> : IDomain<T>
    {
        Task<T> GetEntityAsync();

        IDomain<T> Initialize(string partitionKey, string rowKey);
    }
}
