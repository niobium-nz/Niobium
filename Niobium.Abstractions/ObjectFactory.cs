using Microsoft.Extensions.DependencyInjection;

namespace Niobium
{
    public class ObjectFactory<T>(IServiceProvider serviceProvider) where T : notnull
    {
        public T Build()
        {
            return serviceProvider.GetRequiredService<T>();
        }
    }
}