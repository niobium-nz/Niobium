using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Cod.Channel.Blazor
{
    internal class NavigatorAdaptor : INavigator
    {
        private readonly NavigationManager manager;

        public NavigatorAdaptor(NavigationManager manager) => this.manager = manager;

        public string BaseUri => this.manager.BaseUri;

        public string CurrentUri => this.manager.Uri;

        public Task NavigateToAsync(string url, bool forceLoad = false)
        {
            this.manager.NavigateTo(url, forceLoad: forceLoad);
            return Task.CompletedTask;
        }
    }
}
