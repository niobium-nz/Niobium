using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Niobium.Profile;
using System.Net.Http.Headers;

namespace Niobium.Platform.Profile
{
    internal sealed class ServicePrincipalProfileService<T>(
        IHttpClientFactory httpClientFactory,
        IOptions<ProfileOptions> options,
        ILogger<GenericProfileService<T>> logger)
            : GenericProfileService<T>(httpClientFactory, options, logger), IProfileService<T>
                where T : class, IProfile
    {
        private static readonly TimeSpan clockSkew = TimeSpan.FromMinutes(10);
        private static readonly SemaphoreSlim tokenLock = new(1, 1);
        private static AccessToken? token;

        protected override async Task<HttpClient?> ConfigureHttpClientAsync(HttpClient httpClient, CancellationToken cancellationToken)
        {
            string token = await GetAccessTokenAsync(cancellationToken);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(AuthenticationScheme.BearerLoginScheme, token);
            return httpClient;
        }

        private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            if (token.HasValue && token.Value.ExpiresOn - now > clockSkew)
            {
                return token.Value.Token;
            }

            await tokenLock.WaitAsync(cancellationToken);
            try
            {
                if (!token.HasValue || token.Value.ExpiresOn - now < clockSkew)
                {
                    if (string.IsNullOrWhiteSpace(Options.Value.ProfileAppID))
                    {
                        throw new NotSupportedException($"{Options.Value.ProfileAppID} must be configured in order to use Entra ID authentication based profile service.");
                    }

                    DefaultAzureCredential credential = new();
                    token = await credential.GetTokenAsync(new TokenRequestContext([$"api://{Options.Value.ProfileAppID}/.default"]), cancellationToken);
                }
            }
            finally
            {
                tokenLock.Release();
            }

            return token.Value.Token;
        }
    }
}