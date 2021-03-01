using System.Threading.Tasks;

namespace Cod.Channel
{
    public interface INavigator
    {
        string BaseUri { get; }

        string CurrentUri { get; }

        Task NavigateToAsync(string url, bool forceLoad = false);
    }
}
