namespace Cod.Channel
{
    public interface INavigator
    {
        string BaseUri { get; }

        string CurrentUri { get; }

        void NavigateTo(string url, bool forceLoad = false);
    }
}
