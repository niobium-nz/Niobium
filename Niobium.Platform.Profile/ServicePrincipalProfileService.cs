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
            : GenericProfileService<T>(httpClientFactory, options, logger), IProfileService<T>, IDisposable
                where T : class, IProfile
    {
        private const int ClockSkewMinutes = -10;
        private readonly SemaphoreSlim tokenLock = new(1, 1);
        private AccessToken? token;

        protected override async Task<HttpClient?> ConfigureHttpClientAsync(HttpClient httpClient, CancellationToken cancellationToken)
        {
            string token = await GetAccessTokenAsync(cancellationToken);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(AuthenticationScheme.BearerLoginScheme, token);
            return httpClient;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            if (!token.HasValue || token.Value.ExpiresOn <= now.AddMinutes(ClockSkewMinutes))
            {
                await tokenLock.WaitAsync(cancellationToken);
                if (!token.HasValue || token.Value.ExpiresOn <= now.AddMinutes(ClockSkewMinutes))
                {
                    if (string.IsNullOrWhiteSpace(Options.Value.ProfileAppID))
                    {
                        throw new NotSupportedException($"{Options.Value.ProfileAppID} must be configured in order to use Entra ID authentication based profile service.");
                    }

                    DefaultAzureCredential credential = new();
                    token = await credential.GetTokenAsync(new TokenRequestContext([$"api://{Options.Value.ProfileAppID}/.default"]), cancellationToken);
                }
            }

            return token.Value.Token;
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                tokenLock.Dispose();
            }
        }
    }
}