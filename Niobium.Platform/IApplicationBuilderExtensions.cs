using Microsoft.AspNetCore.Builder;

namespace Niobium.Platform
{
    public static class IApplicationBuilderExtensions
    {
        public static IMiddlewareHost ToMiddlewareHost(this IApplicationBuilder builder) => new ApplicationBuilderMiddlewareHost(builder);
    }
}
