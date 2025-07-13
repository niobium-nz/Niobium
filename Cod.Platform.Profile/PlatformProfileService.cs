using Cod.Profile;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;

namespace Cod.Platform.Profile
{
    public class PlatformProfileService<T>(
        IHttpContextAccessor httpContextAccessor,
        IHttpClientFactory httpClientFactory,
        ILogger<GenericProfileService<T>> logger)
        : GenericProfileService<T>(httpClientFactory, logger), IProfileService<T>
        where T : class, IProfile
    {
        protected override Task<HttpClient?> ConfigureHttpClientAsync(HttpClient httpClient, CancellationToken cancellationToken)
        {
            if (httpContextAccessor.HttpContext == null)
            {
                throw new NotSupportedException($"{ProfileEndpoint} requires an HttpContext to be set.");
            }

            if (!httpContextAccessor.HttpContext.Request.TryParseAuthorizationHeader(Cod.Identity.Constants.IDTokenHeaderKey, out string inputScheme, out string parameter) || inputScheme != AuthenticationScheme.BearerLoginScheme)
            {
                throw new ApplicationException(InternalError.AuthenticationRequired);
            }

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(AuthenticationScheme.BearerLoginScheme, parameter);
            return Task.FromResult<HttpClient?>(httpClient);
        }
    }
}
