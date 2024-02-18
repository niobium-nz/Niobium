using Cod.Platform.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Cod.Platform.Authentication
{
    internal class BearerTokenBuilder : ITokenBuilder
    {
        private readonly Lazy<IConfigurationProvider> configuration;
        private readonly Lazy<IEntitlementStore> store;

        public BearerTokenBuilder(Lazy<IConfigurationProvider> configuration, Lazy<IEntitlementStore> store)
        {
            this.configuration = configuration;
            this.store = store;
        }

        public async Task<string> BuildAsync(
            string mainIdentity,
            IEnumerable<string> roles = null,
            IEnumerable<KeyValuePair<string, string>> entitlements = null,
            ushort validHours = 8)
        {
            List<KeyValuePair<string, string>> dic = new();
            if (entitlements != null)
            {
                dic.AddRange(entitlements);
            }

            if (roles != null)
            {
                foreach (string r in roles)
                {
                    string scope = null;
                    bool isBusinessScoped = false;
                    string[] roleparts = r.Split(Entitlements.ScopeSplitor, StringSplitOptions.RemoveEmptyEntries);
                    IReadOnlyDictionary<string, string> roleEntitlement;
                    if (roleparts.Length == 2)
                    {
                        scope = roleparts[1];
                        isBusinessScoped = Guid.TryParse(scope, out _);
                        roleEntitlement = isBusinessScoped
                            ? store.Value.GetEntitlements(roleparts[0], Entitlements.BusinessScopePlaceholder)
                            : store.Value.GetEntitlements(roleparts[0], Entitlements.CustomScopePlaceholder);
                    }
                    else
                    {
                        roleEntitlement = store.Value.GetEntitlements(r, null);
                    }

                    foreach (string k in roleEntitlement.Keys)
                    {
                        string v = roleEntitlement[k];
                        if (scope != null)
                        {
                            v = isBusinessScoped
                                ? v.Replace(Entitlements.BusinessScopePlaceholder, scope)
                                : v.Replace(Entitlements.CustomScopePlaceholder, scope);
                        }

                        dic.Add(new KeyValuePair<string, string>(k, v));
                    }
                }
            }

            return await BuildAsync(mainIdentity, dic, validHours: validHours);
        }

        private async Task<string> BuildAsync(string mainIdentity, IEnumerable<KeyValuePair<string, string>> entitlements = null, ushort validHours = 8)
        {
            List<Claim> claims = new()
            {
                new Claim(ClaimTypes.Sid, mainIdentity.Trim()),
                new Claim("kid", "0") // for security reasons, newer version of JWT library expects the KID claim exist when validating the token.
            };

            if (entitlements != null)
            {
                claims.AddRange(entitlements.Select(kv => new Claim(kv.Key, kv.Value)));
            }

            string secret = await configuration.Value.GetSettingAsStringAsync(Constant.AUTH_SECRET_NAME);
            SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(secret));
            SigningCredentials creds = new(key, SecurityAlgorithms.HmacSha256);
            JwtSecurityToken token = new(
                issuer: "cod.platform",
                audience: "cod.client",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(validHours < 1 ? 8 : validHours),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
