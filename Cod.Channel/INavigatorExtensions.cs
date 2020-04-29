using System.Collections.Specialized;
using System.Web;

namespace Cod.Channel
{
    public static class INavigatorExtensions
    {
        private readonly static NameValueCollection EmptyQueryString = new NameValueCollection();

        public static NameValueCollection GetQueryStrings(this INavigator navigator)
        {
            var uri = navigator.CurrentUri;
            var index = uri.IndexOf('?');
            if (index >= 0 && uri.Length > index)
            {
                var querystringLength = uri.Length - index - 1;
                if (querystringLength > 0)
                {
                    var querystring = uri.Substring(index + 1, querystringLength);
                    return HttpUtility.ParseQueryString(querystring);
                }
            }

            return EmptyQueryString;
        }
    }
}
