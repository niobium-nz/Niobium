using Microsoft.AspNetCore.Components;

namespace Cod.Channel.Blazor
{
    internal class NavigatorAdaptor(NavigationManager manager) : INavigator
    {
        public string BaseUri => manager.BaseUri;

        public string CurrentUri => manager.Uri;

        public Task NavigateToAsync(string url, bool forceLoad = false)
        {
            manager.NavigateTo(url, forceLoad: forceLoad);
            return Task.CompletedTask;
        }
    }
}
