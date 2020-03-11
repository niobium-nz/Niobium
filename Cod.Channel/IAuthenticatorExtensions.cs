using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
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

        public async static Task<string> GetFullAccountIDAsync(this IAuthenticator authenticator)
        {
            var result = await authenticator.GetClaimsAsync();
            if (!result.IsSuccess)
            {
                if (result.Code == InternalError.AuthenticationRequired)
                {
                    return null;
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            var openID = result.Result[ClaimTypes.NameIdentifier];
            var provider = (OpenIDProvider)Int32.Parse(result.Result[Claims.OPENID_PROVIDER]);
            var appID = result.Result[Claims.OPENID_APP];
            return Account.BuildFullID(provider, appID, openID);
        }

        public static T GetClaim<T>(this IReadOnlyDictionary<string, string> claims, string key)
        {
            if (claims.TryGetClaim(key, out var v))
            {
                return JsonConvert.DeserializeObject<T>(v);
            }
            throw new KeyNotFoundException(key);
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
