using System.Net;

namespace Niobium.Platform
{
    public interface IUnstableWebProxy : IWebProxy
    {
        string Location { get; }

        Task<bool> TestAsync();
    }
}
