using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Cod.Platform.Identity
{
    internal class BearerTokenBuilder(IdentityServiceOptions options) : ITokenBuilder
    {
        public Task<string> BuildAsync(string mainIdentity, IEnumerable<KeyValuePair<string, string>>? entitlements = null, string? audience = null)
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
                    var key = entitlement.Key;
                    if (!key.StartsWith(Constants.ClaimKeyPrefix, StringComparison.Ordinal))
                    {
                        key = $"{Constants.ClaimKeyPrefix}{key}";
                    }
                    claims.Add(key, entitlement.Value);
                }
            }

            SigningCredentials creds;
            if (!string.IsNullOrEmpty(options.AccessTokenSecret))
            {
                creds = new(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.AccessTokenSecret)), SecurityAlgorithms.HmacSha256);
            }
            else
            {
                if (string.IsNullOrEmpty(options.IDTokenPrivateKey))
                {
                    throw new InvalidOperationException($"Either {nameof(options.AccessTokenSecret)} or {nameof(options.IDTokenPrivateKey)} must be provided.");
                }

                var rsa = RSA.Create();
                if (string.IsNullOrEmpty(options.IDTokenPrivateKeyPasscode))
                {
                    rsa.ImportFromPem(options.IDTokenPrivateKey);
                }
                else
                {
                    rsa.ImportFromEncryptedPem(options.IDTokenPrivateKey, options.IDTokenPrivateKeyPasscode);
                }
                creds = new(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256);
            }

            SecurityTokenDescriptor token = new()
            {
                Issuer = options.AccessTokenIssuer,
                Audience = audience ?? options.AccessTokenAudience,
                Claims = claims,
                Expires = DateTime.UtcNow.Add(options.TokenValidity),
                SigningCredentials = creds,
            };

            var result = new JsonWebTokenHandler().CreateToken(token);
            return Task.FromResult(result);
        }
    }
}
