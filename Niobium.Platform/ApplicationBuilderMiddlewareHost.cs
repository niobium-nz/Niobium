using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Niobium.Platform
{
    internal class ApplicationBuilderMiddlewareHost(IApplicationBuilder builder) : IMiddlewareHost
    {
        public void UseMiddleware<TMiddleware>() where TMiddleware : IMiddleware
        {
            builder.UsePlatform();
            builder.UseMiddleware<TMiddleware>();
        }
    }
}
