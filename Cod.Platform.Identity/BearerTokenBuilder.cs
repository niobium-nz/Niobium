using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Cod.Platform.Identity
{
    internal sealed class BearerTokenBuilder(IOptions<IdentityServiceOptions> options) : ITokenBuilder
    {
        public Task<string> BuildAsync(
            string mainIdentity,
            IEnumerable<KeyValuePair<string, string>>? entitlements = null,
            string? audience = null,
            IEnumerable<string>? roles = null)
        {
            Dictionary<string, object> claims = new()
            {
                { ClaimTypes.NameIdentifier, mainIdentity.Trim() },
                { ClaimTypes.Sid, mainIdentity.Trim() },
                { "kid", "0" }, // for security reasons, newer version of JWT library expects the KID claim exist when validating the token.
            };

            if (entitlements != null)
            {
                foreach (KeyValuePair<string, string> entitlement in entitlements)
                {
                    string key = entitlement.Key;
                    if (!key.StartsWith(Constants.ClaimKeyPrefix, StringComparison.Ordinal))
                    {
                        key = $"{Constants.ClaimKeyPrefix}{key}";
                    }
                    claims.Add(key, entitlement.Value);
                }
            }

            if (roles != null)
            {
                foreach (string role in roles)
                {
                    claims.Add(ClaimTypes.Role, role);
                }
            }

            SigningCredentials creds;
            if (!string.IsNullOrEmpty(options.Value.AccessTokenSecret))
            {
                creds = new(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Value.AccessTokenSecret)), SecurityAlgorithms.HmacSha256);
            }
            else
            {
                if (string.IsNullOrEmpty(options.Value.IDTokenPrivateKey))
                {
                    throw new InvalidOperationException($"Either {nameof(options.Value.AccessTokenSecret)} or {nameof(options.Value.IDTokenPrivateKey)} must be provided.");
                }

                RSA rsa = RSA.Create();
                if (string.IsNullOrEmpty(options.Value.IDTokenPrivateKeyPasscode))
                {
                    rsa.ImportFromPem(options.Value.IDTokenPrivateKey);
                }
                else
                {
                    rsa.ImportFromEncryptedPem(options.Value.IDTokenPrivateKey, options.Value.IDTokenPrivateKeyPasscode);
                }
                creds = new(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256);
            }

            SecurityTokenDescriptor token = new()
            {
                Issuer = options.Value.AccessTokenIssuer,
                Audience = audience ?? options.Value.AccessTokenAudience,
                Claims = claims,
                Expires = DateTime.UtcNow.Add(options.Value.TokenValidity),
                SigningCredentials = creds,
            };

            string result = new JsonWebTokenHandler().CreateToken(token);
            return Task.FromResult(result);
        }
    }
}
