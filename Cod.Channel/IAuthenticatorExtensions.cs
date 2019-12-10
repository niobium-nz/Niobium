using Newtonsoft.Json;

namespace Cod.Channel
{
    public static class IAuthenticatorExtensions
    {
        public static bool IsAuthenticated(this IAuthenticator authenticator)
            => authenticator != null && authenticator.Token != null && authenticator.Token.Validate();

        public static bool TryGetClaim(this IAuthenticator authenticator, string key, out string value)
        {
            if (authenticator.Claims.ContainsKey(key))
            {
                value = authenticator.Claims[key];
                return true;
            }
            value = null;
            return false;
        }

        public static bool TryGetClaim<T>(this IAuthenticator authenticator, string key, out T value)
        {
            if (authenticator.TryGetClaim(key, out var v))
            {
                value = JsonConvert.DeserializeObject<T>(v);
                return true;
            }
            value = default;
            return false;
        }
    }
}
