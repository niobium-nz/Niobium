using Microsoft.Extensions.DependencyInjection;

namespace Cod.Channel
{
    public interface IDependencyModule
    {
        void Load(IServiceCollection services);
    }
}
