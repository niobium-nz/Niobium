using Microsoft.AspNetCore.Components;

namespace Cod.Channel.Blazor
{
    internal class NavigatorAdaptor : INavigator
    {
        private readonly NavigationManager manager;

        public NavigatorAdaptor(NavigationManager manager) => this.manager = manager;

        public string BaseUri => this.manager.BaseUri;

        public string CurrentUri => this.manager.Uri;

        public void NavigateTo(string url) => this.manager.NavigateTo(url);
    }
}
