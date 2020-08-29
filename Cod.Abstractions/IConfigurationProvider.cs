using System.Threading.Tasks;

namespace Cod
{
    public interface IConfigurationProvider
    {
        string GetSettingAsString(string key, bool cache = true);

        Task<string> GetSettingAsStringAsync(string key, bool cache = true);
    }
}
