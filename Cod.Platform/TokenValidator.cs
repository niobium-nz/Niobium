using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Cod.Contract;
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

        public async Task<string> CreateAsync(string nameIdentifier, string contact, string openIDProvider, string openIDApp,
            IEnumerable<string> roles = null, IEnumerable<KeyValuePair<string, string>> entitlements = null)
        {
            var dic = new Dictionary<string, string>();
            if (entitlements != null)
            {
                foreach (var item in entitlements)
                {
                    if (dic.ContainsKey(item.Key))
                    {
                        dic[item.Key] = $"{dic[item.Key]},{item.Value}";
                    }
                    else
                    {
                        dic.Add(item.Key, item.Value);
                    }
                }
            }

            if (roles != null)
            {
                foreach (var role in roles)
                {
                    var roleEntitlement = this.store.Value.Get(role);
                    foreach (var k in roleEntitlement.Keys)
                    {
                        if (dic.ContainsKey(k))
                        {
                            var exist = dic[k].Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                            var current = roleEntitlement[k].Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                            var values = exist.Concat(current).Distinct();
                            dic[k] = String.Join(",", values);
                        }
                        else
                        {
                            dic.Add(k, roleEntitlement[k]);
                        }
                    }
                }
            }

            return await this.BuildAsync(nameIdentifier, contact, openIDProvider, openIDApp, dic);
        }

        private async Task<string> BuildAsync(string nameIdentifier, string contact,
            string openIDProvider, string openIDApp, IReadOnlyDictionary<string, string> entitlements = null)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, nameIdentifier)
            };
            if (!String.IsNullOrWhiteSpace(contact))
            {
                claims.Add(new Claim(ClaimsPrincipalExtensions.ACCOUNT_CONTACT, contact.Trim()));
                claims.Add(new Claim(ClaimTypes.MobilePhone, contact.Trim()));
            }

            if (!String.IsNullOrWhiteSpace(openIDProvider) && !String.IsNullOrWhiteSpace(openIDApp))
            {
                claims.Add(new Claim(ClaimsPrincipalExtensions.OPENID_PROVIDER, openIDProvider.Trim()));
                claims.Add(new Claim(ClaimsPrincipalExtensions.OPENID_APP, openIDApp.Trim()));
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
