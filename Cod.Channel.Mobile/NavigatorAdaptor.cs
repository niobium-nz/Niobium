using Xamarin.Forms;

namespace Cod.Channel.Mobile
{
    internal class NavigatorAdaptor : INavigator
    {
        public string BaseUri => String.Empty;

        public string CurrentUri => Shell.Current.CurrentState.Location.ToString();

        public async Task NavigateToAsync(string url, bool forceLoad = false, CancellationToken? cancellationToken = default)
        {
            if (forceLoad && !url.StartsWith("//", StringComparison.InvariantCulture))
            {
                url = $"//{url}";
            }

            await Shell.Current.GoToAsync(url).ConfigureAwait(false);
        }
    }
}
