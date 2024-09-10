using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Cod.Platform.Identity
{
    internal class BearerTokenBuilder(IConfiguration configuration) : ITokenBuilder
    {
        public Task<string> BuildAsync(
            string mainIdentity,
            IEnumerable<KeyValuePair<string, string>>? entitlements = null,
            string? symmetricSecurityKey = null,
            ushort validHours = 8,
            string audience = Constants.IDTokenDefaultAudience,
            string issuer = Constants.IDTokenDefaultIssuer)
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
                    claims.Add(entitlement.Key, entitlement.Value);
                }
            }

            SigningCredentials creds;
            if (!string.IsNullOrEmpty(symmetricSecurityKey))
            {
                creds = new(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(symmetricSecurityKey)), SecurityAlgorithms.HmacSha256);
            }
            else
            {
                string privateKey = configuration.GetValue<string>(Constants.IDTokenPrivateKey);
                string privateKeyPasscode = configuration.GetValue<string>(Constants.IDTokenPrivateKeyPasscode);
                if (string.IsNullOrEmpty(privateKey) || string.IsNullOrEmpty(privateKeyPasscode))
                {
                    throw new ApplicationException(Cod.InternalError.InternalServerError);
                }

                var rsa = RSA.Create();
                rsa.ImportFromEncryptedPem(privateKey, privateKeyPasscode);
                creds = new(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256);
            }

            SecurityTokenDescriptor token = new()
            {
                Issuer = issuer,
                Audience = audience,
                Claims = claims,
                Expires = DateTime.UtcNow.AddHours(validHours < 1 ? 8 : validHours),
                SigningCredentials = creds,
            };

            var result = new JsonWebTokenHandler().CreateToken(token);
            return Task.FromResult(result);
        }
    }
}
