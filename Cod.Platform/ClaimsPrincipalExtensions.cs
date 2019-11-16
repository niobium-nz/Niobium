using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Claims;

namespace Cod.Platform
{
    public static class ClaimsPrincipalExtensions
    {
        public const string ACCOUNT_CONTACT = nameof(ACCOUNT_CONTACT);
        public const string OPENID_PROVIDER = nameof(OPENID_PROVIDER);
        public const string OPENID_APP = nameof(OPENID_APP);

        public static string GetContactClaim(this ClaimsPrincipal principal)
            => principal.GetClaim<string>(ACCOUNT_CONTACT);

        public static T GetClaim<T>(this ClaimsPrincipal principal, string key)
        {
            if (!TryGetClaim<T>(principal, key, out var result))
            {
                throw new KeyNotFoundException($"The specified claim does not exist: {key}.");
            }
            return result;
        }

        public static bool TryGetClaim<T>(this ClaimsPrincipal principal, string key, out T result)
        {
            var claim = principal.Claims.SingleOrDefault(c => c.Type == key);
            if (claim == null)
            {
                result = default;
                return false;
            }
            var converter = TypeDescriptor.GetConverter(typeof(T));
            result = (T)converter.ConvertFrom(claim.Value);
            return true;
        }
    }
}
