using System.Threading.Tasks;

namespace Cod.Platform
{
    public interface IAccountable
    {
        ICacheStore CacheStore { get; }

        Task<string> GetAccountingPrincipalAsync();
    }
}
