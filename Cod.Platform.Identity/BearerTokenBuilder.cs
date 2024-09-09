using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Cod.Platform.Identity
{
    internal class BearerTokenBuilder : ITokenBuilder
    {
        private readonly IConfiguration configuration;

        public BearerTokenBuilder(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public Task<string> BuildAsync(
            string mainIdentity,
            IEnumerable<KeyValuePair<string, string>> entitlements = null,
            ushort validHours = 8,
            string audience = "cod.client",
            string issuer = Constants.IDTokenIssuer)
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
            IDisposable key = null;
            string secret = configuration.GetValue<string>(Cod.Platform.Constants.AccessTokenSecret);
            if (!string.IsNullOrEmpty(secret))
            {
                creds = new(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)), SecurityAlgorithms.HmacSha256);
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
                key = rsa;
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
            key?.Dispose();
            return Task.FromResult(result);
        }
    }
}
