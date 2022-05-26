using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Cod.Platform
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
            var dic = new List<KeyValuePair<string, string>>();
            if (entitlements != null)
            {
                dic.AddRange(entitlements);
            }

            if (roles != null)
            {
                foreach (var r in roles)
                {
                    string scope = null;
                    var isBusinessScoped = false;
                    var roleparts = r.Split(Entitlements.ScopeSplitor, StringSplitOptions.RemoveEmptyEntries);
                    IReadOnlyDictionary<string, string> roleEntitlement;
                    if (roleparts.Length == 2)
                    {
                        scope = roleparts[1];
                        isBusinessScoped = Guid.TryParse(scope, out _);
                        if (isBusinessScoped)
                        {
                            roleEntitlement = this.store.Value.GetEntitlements(roleparts[0], Entitlements.BusinessScopePlaceholder);
                        }
                        else
                        {
                            roleEntitlement = this.store.Value.GetEntitlements(roleparts[0], Entitlements.CustomScopePlaceholder);
                        }
                    }
                    else
                    {
                        roleEntitlement = this.store.Value.GetEntitlements(r, null);
                    }

                    foreach (var k in roleEntitlement.Keys)
                    {
                        var v = roleEntitlement[k];
                        if (scope != null)
                        {
                            if (isBusinessScoped)
                            {
                                v = v.Replace(Entitlements.BusinessScopePlaceholder, scope);
                            }
                            else
                            {
                                v = v.Replace(Entitlements.CustomScopePlaceholder, scope);
                            }
                        }

                        dic.Add(new KeyValuePair<string, string>(k, v));
                    }
                }
            }

            return await this.BuildAsync(mainIdentity, dic, validHours: validHours);
        }

        private async Task<string> BuildAsync(string mainIdentity, IEnumerable<KeyValuePair<string, string>> entitlements = null, ushort validHours = 8)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Sid, mainIdentity.Trim())
            };

            if (entitlements != null)
            {
                claims.AddRange(entitlements.Select(kv => new Claim(kv.Key, kv.Value)));
            }

            var secret = await this.configuration.Value.GetSettingAsStringAsync(Constant.AUTH_SECRET_NAME);
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: "cod.platform",
                audience: "cod.client",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(validHours < 1 ? 8 : validHours),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
