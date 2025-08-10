using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Web;

namespace Cod.Channel
{
    public static class INavigatorExtensions
    {
        private static readonly NameValueCollection EmptyQueryString = [];

        public static async Task CheckAndPerformGotoAsync(this INavigator navigator)
        {
            NameValueCollection queries = navigator.GetQueryStrings();
            string? go = queries.Get("go");
            if (!string.IsNullOrWhiteSpace(go))
            {
                queries.Remove("go");
                string? queryString = queries.ToString();
                go = WebUtility.UrlDecode(go);
                await navigator.NavigateToAsync($"{navigator.BaseUri}{go}?{queryString}");
            }
        }

        public static NameValueCollection GetQueryStrings(this INavigator navigator)
        {
            string uri = navigator.CurrentUri;
            int index = uri.IndexOf('?');
            if (index >= 0 && uri.Length > index)
            {
                int querystringLength = uri.Length - index - 1;
                if (querystringLength > 0)
                {
                    string querystring = uri.Substring(index + 1, querystringLength);
                    return HttpUtility.ParseQueryString(querystring);
                }
            }

            return EmptyQueryString;
        }

        public static bool TryGetQueryString(this INavigator navigator, string key, [NotNullWhen(true)] out string? value)
        {
            NameValueCollection queries = navigator.GetQueryStrings();
            value = queries.Get(key);
            return !string.IsNullOrWhiteSpace(value);
        }
    }
}
