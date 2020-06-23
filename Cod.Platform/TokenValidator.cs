using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;

namespace Cod.Platform
{
    internal class TokenValidator : ITokenManager
    {
        private readonly Lazy<IConfigurationProvider> configuration;
        private readonly Lazy<IEntitlementStore> store;

        public TokenValidator(Lazy<IConfigurationProvider> configuration, Lazy<IEntitlementStore> store)
        {
            this.configuration = configuration;
            this.store = store;
        }

        public async Task<string> CreateAsync(Guid? group, Guid? user, string nameIdentifier, string contact, string openIDProvider, string openIDApp,
            IEnumerable<string> roles = null, IEnumerable<KeyValuePair<string, string>> entitlements = null)
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
                            roleEntitlement = this.store.Value.Get(roleparts[0], Entitlements.BusinessScopePlaceholder);
                        }
                        else
                        {
                            roleEntitlement = this.store.Value.Get(roleparts[0], Entitlements.CustomScopePlaceholder);
                        }
                    }
                    else
                    {
                        roleEntitlement = this.store.Value.Get(r, null);
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

            return await this.BuildAsync(group, user, nameIdentifier, contact, openIDProvider, openIDApp, dic);
        }

        private async Task<string> BuildAsync(Guid? group, Guid? user, string nameIdentifier, string contact,
            string openIDProvider, string openIDApp, IEnumerable<KeyValuePair<string, string>> entitlements = null)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, nameIdentifier),
            };
            if (user.HasValue)
            {
                claims.Add(new Claim(ClaimTypes.Sid, user.Value.ToKey()));
            }
            if (group.HasValue)
            {
                claims.Add(new Claim(ClaimTypes.GroupSid, group.Value.ToKey()));
            }
            if (!String.IsNullOrWhiteSpace(contact))
            {
                claims.Add(new Claim(Claims.ACCOUNT_CONTACT, contact.Trim()));
                claims.Add(new Claim(ClaimTypes.MobilePhone, contact.Trim()));
            }

            if (!String.IsNullOrWhiteSpace(openIDProvider) && !String.IsNullOrWhiteSpace(openIDApp))
            {
                claims.Add(new Claim(Claims.OPENID_PROVIDER, openIDProvider.Trim()));
                claims.Add(new Claim(Claims.OPENID_APP, openIDApp.Trim()));
            }

            if (entitlements != null)
            {
                claims.AddRange(entitlements.Select(kv => new Claim(kv.Key, kv.Value)));
            }

            var secret = await this.configuration.Value.GetSettingAsync(Constant.AUTH_SECRET_NAME);
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: "cod.platform",
                audience: "cod.client",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
