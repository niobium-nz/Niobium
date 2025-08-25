using Microsoft.Extensions.DependencyInjection;

namespace Niobium.Channel
{
    public interface IDependencyModule
    {
        void Load(IServiceCollection services);
    }
}
