using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Niobium.Profile;
using System.Net.Http.Headers;

namespace Niobium.Platform.Profile
{
    internal sealed class ClientTokenProfileService<T>(
        IHttpContextAccessor httpContextAccessor,
        IHttpClientFactory httpClientFactory,
        IOptions<ProfileOptions> options,
        ILogger<GenericProfileService<T>> logger)
            : GenericProfileService<T>(httpClientFactory, options, logger), IProfileService<T>
            where T : class, IProfile
    {
        protected override Task<HttpClient?> ConfigureHttpClientAsync(HttpClient httpClient, CancellationToken cancellationToken)
        {
            if (httpContextAccessor.HttpContext == null)
            {
                throw new NotSupportedException($"{ProfileEndpoint} requires an HttpContext to be set.");
            }

            if (!httpContextAccessor.HttpContext.Request.TryParseAuthorizationHeader(Identity.Constants.IDTokenHeaderKey, out string inputScheme, out string parameter) || inputScheme != AuthenticationScheme.BearerLoginScheme)
            {
                throw new ApplicationException(Niobium.InternalError.AuthenticationRequired);
            }

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(AuthenticationScheme.BearerLoginScheme, parameter);
            return Task.FromResult<HttpClient?>(httpClient);
        }
    }
}
