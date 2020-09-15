using Microsoft.Extensions.Configuration;

namespace Cod.Platform
{
    public interface IPlatformConfigurationProvider : IConfigurationProvider
    {
        IConfiguration Configuration { get; }
    }
}
