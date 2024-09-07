using Cod.Platform.Identity.Authorization;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Cod.Platform.Identity.Authentication
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
            ushort validHours = 8, string audience = "cod.client")
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

            return await BuildAsync(mainIdentity, audience, dic, validHours: validHours);
        }

        private async Task<string> BuildAsync(string mainIdentity, string audience, IEnumerable<KeyValuePair<string, string>> entitlements = null, ushort validHours = 8)
        {
            Dictionary<string, object> claims = new()
            {
                { ClaimTypes.Sid, mainIdentity.Trim() },
                { "kid", "0" }, // for security reasons, newer version of JWT library expects the KID claim exist when validating the token.
            };

            if (entitlements != null)
            {
                foreach (KeyValuePair<string, string> entitlement in entitlements)
                {
                    claims.Add(entitlement.Key, entitlement.Value);
                }
            }

            SigningCredentials creds;
            string secret = await configuration.Value.GetSettingAsStringAsync(Constants.AUTH_SECRET_NAME);
            if (secret == null)
            {
                SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(secret));
                creds = new(key, SecurityAlgorithms.HmacSha256);
            }
            else
            {
                string privateKey = await configuration.Value.GetSettingAsStringAsync(Constants.IDTokenPrivateKey);
                string privateKeyPasscode = await configuration.Value.GetSettingAsStringAsync(Constants.IDTokenPrivateKeyPasscode);
                if (string.IsNullOrEmpty(privateKey) || string.IsNullOrEmpty(privateKeyPasscode))
                {
                    throw new ApplicationException(InternalError.InternalServerError);
                }

                var buff = Convert.FromBase64String(privateKey);
                using var rsa = RSA.Create();
                rsa.ImportEncryptedPkcs8PrivateKey(privateKeyPasscode, buff, out _);
                creds = new(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256);
            }
            
            SecurityTokenDescriptor token = new()
            {
                Issuer = "cod.platform",
                Audience = audience,
                Claims = claims,
                Expires = DateTime.UtcNow.AddHours(validHours < 1 ? 8 : validHours),
                SigningCredentials = creds,
            };

            return new JsonWebTokenHandler().CreateToken(token);
        }
    }
}
