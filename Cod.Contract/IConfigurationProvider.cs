using System.Threading.Tasks;

namespace Cod.Contract
{
    public interface IConfigurationProvider
    {
        Task<string> GetSettingAsync(string key, bool cache = true);
    }
}
