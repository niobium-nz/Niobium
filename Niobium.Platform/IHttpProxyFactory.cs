namespace Niobium.Platform
{
    public interface IHttpProxyFactory
    {
        Task<IUnstableWebProxy> CreateAsync(string location);
    }
}
