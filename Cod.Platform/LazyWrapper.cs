using Microsoft.Extensions.DependencyInjection;

namespace Cod.Platform
{
    internal sealed class LazyWrapper<T> : Lazy<T> where T : class
    {
        public LazyWrapper(IServiceProvider provider)
            : base(() => provider.GetRequiredService<T>())
        {
        }
    }
}
