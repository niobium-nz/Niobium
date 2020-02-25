using System.Net;
using System.Threading.Tasks;

namespace Cod.Platform
{
    public interface IUnstableWebProxy : IWebProxy
    {
        string Location { get; }

        Task<bool> TestAsync();
    }
}
