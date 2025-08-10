using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Cod.Platform.Identity
{
    public class PrincipalParser(IOptions<IdentityServiceOptions> options)
    {
        public async Task<ClaimsPrincipal> ParseIDPrincipalAsync(string bearerToken, string? audience, CancellationToken cancellationToken = default)
        {
            try
            {
                RSA rsa = RSA.Create();
                rsa.ImportFromPem(options.Value.IDTokenPublicKey);
                RsaSecurityKey key = new(rsa)
                {
                    KeyId = "0"
                };
                string issuer = options.Value.IDTokenIssuer;
                return await ValidateAndDecodeJWTAsync(bearerToken, key, issuer, audience, cancellationToken);
            }
            catch (Exception)
            {
                throw new ApplicationException(InternalError.AuthenticationRequired);
            }
        }

        public async Task<ClaimsPrincipal> ParseAsync(string bearerToken, CancellationToken cancellationToken = default)
        {
            try
            {
                SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(options.Value.AccessTokenSecret!))
                {
                    KeyId = "0"
                };
                string issuer = options.Value.AccessTokenIssuer;
                string audience = options.Value.AccessTokenAudience;
                return await ValidateAndDecodeJWTAsync(bearerToken, key, issuer, audience, cancellationToken);
            }
            catch (Exception)
            {
                throw new ApplicationException(InternalError.AuthenticationRequired);
            }
        }

        private static async Task<ClaimsPrincipal> ValidateAndDecodeJWTAsync(string jwt, SecurityKey key, string issuer, string? audience, CancellationToken cancellationToken)
        {
            TokenValidationParameters validationParameters = new()
            {
                ClockSkew = TimeSpan.FromMinutes(5),
                IssuerSigningKey = key,
                RequireSignedTokens = true,
                RequireExpirationTime = true,
                ValidateLifetime = true,
                ValidateIssuer = true,
                ValidIssuer = issuer
            };

            if (!string.IsNullOrWhiteSpace(audience))
            {
                validationParameters.ValidAudience = audience;
                validationParameters.RequireAudience = true;
                validationParameters.ValidateAudience = true;
            }
            else
            {
                validationParameters.RequireAudience = false;
                validationParameters.ValidateAudience = false;
            }

            try
            {
                TokenValidationResult validationResult = await new JsonWebTokenHandler().ValidateTokenAsync(jwt, validationParameters);
                return !validationResult.IsValid
                    ? throw new ApplicationException(InternalError.AuthenticationRequired, innerException: validationResult.Exception)
                    : new ClaimsPrincipal(validationResult.ClaimsIdentity);
            }
            catch (SecurityTokenValidationException stvex)
            {
                throw new InvalidDataException($"Token failed validation: {stvex.Message}");
            }
            catch (ArgumentException argex)
            {
                throw new ArgumentException($"Token was invalid: {argex.Message}");
            }
        }
    }
}
