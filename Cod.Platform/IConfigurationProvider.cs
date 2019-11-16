using System.Threading.Tasks;

namespace Cod.Platform
{
    public interface IConfigurationProvider
    {
        Task<string> GetSettingAsync(string key, bool cache = true);
    }
}
