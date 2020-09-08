using System.Threading.Tasks;

namespace Cod.Channel
{
    public interface IBrowser
    {
        Task<string> GetUserAgentAsync();

        Task<string> GetLocateAsync();
    }
}
