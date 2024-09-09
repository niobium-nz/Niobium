using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;

namespace Cod.Platform.Identity
{
    internal class AccessTokenMiddleware : IMiddleware
    {
        private readonly ITokenBuilder tokenBuilder;
        private readonly IConfiguration configuration;
        private readonly ILogger logger;

        public AccessTokenMiddleware(ITokenBuilder tokenBuilder, IConfiguration configuration, ILogger<AccessTokenMiddleware> logger)
        {
            this.tokenBuilder = tokenBuilder;
            this.configuration = configuration;
            this.logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var req = context.Request;
            if (!req.Path.HasValue || req.Path.Value.Equals(Constants.AuthenticateEndpoint, StringComparison.OrdinalIgnoreCase))
            {
                await next(context);
                return;
            }

            var authHeader = req.Headers.Authorization.SingleOrDefault();
            if (!AuthenticationHeaderValue.TryParse(authHeader, out var value))
            {
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                return;
            }

            if (value.Scheme != AuthenticationScheme.BearerLoginScheme)
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return;
            }

            var pubKey = configuration.GetValue<string>(Constants.IDTokenPublicKey);
            if (string.IsNullOrEmpty(pubKey))
            {
                logger.LogError($"Missing ID token public key: {Constants.IDTokenPublicKey}");
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return;
            }

            var audience = configuration.GetValue<string>(Constants.IDTokenAudience);
            if (string.IsNullOrEmpty(audience))
            {
                logger.LogError($"Missing ID token audience setting: {Constants.IDTokenAudience}");
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return;
            }

            var rsa = RSA.Create();
            rsa.ImportFromPem(pubKey);
            var key = new RsaSecurityKey(rsa);
            var principal = await HttpRequestExtensions.ValidateAndDecodeJWTAsync(value.Parameter, key, Constants.IDTokenIssuer, audience);
            if (principal == null)
            {
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                return;
            }

            if (!principal.TryGetClaim<Guid>(ClaimTypes.Sid, out var sid))
            {
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                return;
            }

            var token = await tokenBuilder.BuildAsync(sid.ToString(), audience: audience, issuer: audience);
            req.DeliverAuthenticationToken(token, AuthenticationScheme.BearerLoginScheme);
            context.Response.StatusCode = (int)HttpStatusCode.OK;
        }
    }
}
