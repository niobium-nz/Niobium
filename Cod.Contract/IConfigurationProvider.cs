using System.Threading.Tasks;

namespace Cod
{
    public interface IConfigurationProvider
    {
        Task<string> GetSettingAsync(string key, bool cache = true);
    }
}
