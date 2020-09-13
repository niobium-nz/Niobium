using System.Threading.Tasks;

namespace Cod.Channel
{
    public interface IBootstrapper
    {
        Task InitializeAsync();
    }
}
