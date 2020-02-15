using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Claims;
using Newtonsoft.Json;

namespace Cod.Channel
{
    public static class ClaimsPrincipalExtensions
    {
        public static string GetContactClaim(this ClaimsPrincipal principal)
            => principal.GetClaim<string>(Claims.ACCOUNT_CONTACT);

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
            if (typeof(IConvertible).IsAssignableFrom(typeof(T)))
            {
                var converter = TypeDescriptor.GetConverter(typeof(T));
                result = (T)converter.ConvertFrom(claim.Value);
                return true;
            }
            else
            {
                result = JsonConvert.DeserializeObject<T>(claim.Value);
                return true;
            }
        }
    }
}
