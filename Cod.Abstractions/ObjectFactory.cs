using Microsoft.Extensions.DependencyInjection;

namespace Cod
{
    public class ObjectFactory<T>(IServiceProvider serviceProvider) where T : notnull
    {
        public T Build() => serviceProvider.GetRequiredService<T>();
    }
}