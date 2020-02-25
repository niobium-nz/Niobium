using System.Threading.Tasks;

namespace Cod.Platform
{
    public interface IHttpProxyFactory
    {
        Task<IUnstableWebProxy> CreateAsync(string location);
    }
}
