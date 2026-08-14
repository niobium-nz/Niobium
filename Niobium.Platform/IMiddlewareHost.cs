using Microsoft.AspNetCore.Http;

namespace Niobium.Platform
{
    public interface IMiddlewareHost
    {
        void UseMiddleware<TMiddleware>() where TMiddleware : IMiddleware;
    }
}
