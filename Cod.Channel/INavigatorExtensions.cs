using System;
using System.Collections.Specialized;
using System.Net;
using System.Threading.Tasks;
using System.Web;

namespace Cod.Channel
{
    public static class INavigatorExtensions
    {
        private readonly static NameValueCollection EmptyQueryString = new NameValueCollection();

        public async static Task CheckAndPerformGotoAsync(this INavigator navigator)
        {
            var queries = navigator.GetQueryStrings();
            var go = queries.Get("go");
            if (!String.IsNullOrWhiteSpace(go))
            {
                queries.Remove("go");
                var queryString = queries.ToString();
                go = WebUtility.UrlDecode(go);
                await navigator.NavigateToAsync($"{navigator.BaseUri}{go}?{queryString}");
            }
        }

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
