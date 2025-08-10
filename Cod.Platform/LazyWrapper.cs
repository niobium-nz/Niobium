using Microsoft.Extensions.DependencyInjection;

namespace Cod.Platform
{
    public sealed class LazyWrapper<T>(IServiceProvider provider) : Lazy<T>(provider.GetRequiredService<T>) where T : class
    {
    }
}
