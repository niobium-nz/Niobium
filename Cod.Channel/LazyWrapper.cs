using Microsoft.Extensions.DependencyInjection;

namespace Cod.Channel
{
    internal sealed class LazyWrapper<T>(IServiceProvider provider) : Lazy<T>(() => provider.GetRequiredService<T>()) where T : class
    {
    }
}
