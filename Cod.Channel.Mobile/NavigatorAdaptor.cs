using System;

namespace Cod.Channel.Mobile
{
    internal class NavigatorAdaptor : INavigator
    {
        public string BaseUri => String.Empty;

        public string CurrentUri => String.Empty;

        public void NavigateTo(string url, bool forceLoad = false)
        { }
    }
}
