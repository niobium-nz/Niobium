using Cod.Identity;
using Cod.Profile;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;

namespace Cod.Channel.Profile
{
    public class ChannelProfileService<T>(
        IAuthenticator authenticator,
        IHttpClientFactory httpClientFactory,
        IOptions<ProfileOptions> options,
        ILogger<GenericProfileService<T>> logger)
        : GenericProfileService<T>(httpClientFactory, options, logger), IProfileService<T>
            where T : class, IProfile
    {
        protected async override Task<HttpClient?> ConfigureHttpClientAsync(HttpClient httpClient, CancellationToken cancellationToken)
        {
            var authenticated = await authenticator.GetAuthenticateStatus(cancellationToken);
            if (!authenticated || authenticator.IDToken == null || string.IsNullOrWhiteSpace(authenticator.IDToken.EncodedToken))
            {
                return null;
            }

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(AuthenticationScheme.BearerLoginScheme, authenticator.IDToken.EncodedToken);
            return httpClient;
        }
    }
}
