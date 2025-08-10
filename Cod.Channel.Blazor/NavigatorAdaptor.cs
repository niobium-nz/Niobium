using Microsoft.AspNetCore.Components;

namespace Cod.Channel.Blazor
{
    internal sealed class NavigatorAdaptor(NavigationManager manager) : INavigator
    {
        public string BaseUri => manager.BaseUri;

        public string CurrentUri => manager.Uri;

        public Task NavigateToAsync(string url, bool forceLoad = false, CancellationToken? cancellationToken = default)
        {
            manager.NavigateTo(url, forceLoad: forceLoad);
            return Task.CompletedTask;
        }
    }
}
