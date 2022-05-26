using System.Net;

namespace Cod.Platform
{
    public interface IUnstableWebProxy : IWebProxy
    {
        string Location { get; }

        Task<bool> TestAsync();
    }
}
