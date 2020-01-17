using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;

namespace Cod.Channel
{
    public static class IAuthenticatorExtensions
    {
        public static bool IsAuthenticated(this IAuthenticator authenticator)
            => authenticator != null && authenticator.Token != null && authenticator.Token.Validate();

        public static bool TryGetClaim(this IReadOnlyDictionary<string, string> claims, string key, out string value)
        {
            if (claims.ContainsKey(key))
            {
                value = claims[key];
                return true;
            }
            value = null;
            return false;
        }

        public static bool TryGetClaim<T>(this IReadOnlyDictionary<string, string> claims, string key, out T value)
        {
            if (claims.TryGetClaim(key, out var v))
            {
                value = JsonConvert.DeserializeObject<T>(v);
                return true;
            }
            value = default;
            return false;
        }
    }
}
