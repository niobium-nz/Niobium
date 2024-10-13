using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace Cod.Platform.Identity
{
    public static class AccessTokenHelper
    {
        public static async Task<ClaimsPrincipal> TryParsePrincipalAsync(string bearerToken, SecurityKey? key = null, string? issuer = null, string? audience = null, CancellationToken cancellationToken = default)
        {
            if (IdentityServiceOptions.Instance == null)
            {
                throw new InvalidOperationException($"'{nameof(DependencyModule.AddIdentity)}' must be called at startup.");
            }

            if (IdentityServiceOptions.Instance.AccessTokenSecret == null)
            {
                throw new InvalidOperationException($"'{nameof(IdentityServiceOptions.AccessTokenSecret)}' must be configured at startup.");
            }

            try
            {
                key ??= new SymmetricSecurityKey(Encoding.UTF8.GetBytes(IdentityServiceOptions.Instance.AccessTokenSecret));
                key.KeyId = "0";
                issuer ??= IdentityServiceOptions.Instance.AccessTokenIssuer;
                audience ??= IdentityServiceOptions.Instance.AccessTokenAudience;
                return await ValidateAndDecodeJWTAsync(bearerToken, key, issuer, audience, cancellationToken);
            }
            catch (Exception)
            {
                throw new ApplicationException(InternalError.AuthenticationRequired);
            }
        }

        private static async Task<ClaimsPrincipal> ValidateAndDecodeJWTAsync(string jwt, SecurityKey key, string issuer, string audience, CancellationToken cancellationToken)
        {
            TokenValidationParameters validationParameters = new()
            {
                ClockSkew = TimeSpan.FromMinutes(5),
                IssuerSigningKey = key,
                RequireSignedTokens = true,
                RequireExpirationTime = true,
                ValidateLifetime = true,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateIssuer = true,
                ValidIssuer = issuer
            };

            try
            {
                TokenValidationResult validationResult = await new JsonWebTokenHandler().ValidateTokenAsync(jwt, validationParameters);
                if (!validationResult.IsValid)
                {
                    throw new ApplicationException(InternalError.AuthenticationRequired, innerException: validationResult.Exception);
                }

                return new ClaimsPrincipal(validationResult.ClaimsIdentity);
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
