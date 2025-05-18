using Cod.Identity;
using Cod.Profile;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Cod.Channel.Profile
{

    public class GenericProfileService<T>(IAuthenticator authenticator, IHttpClientFactory httpClientFactory, ILogger<GenericProfileService<T>> logger)
        : IProfileService<T> where T : IProfile
    {
        public virtual string ProfileEndpoint { get => Cod.Profile.Constants.DefaultProfileEndpoint; }

        private async Task<HttpClient?> GetHttpClientAsync(CancellationToken cancellationToken)
        {
            var httpClient = httpClientFactory.CreateClient(ProfileOptions.DefaultHttpClientName);

            var authenticated = await authenticator.GetAuthenticateStatus(cancellationToken);
            if (!authenticated)
            {
                return null;
            }

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authenticator.AccessToken!.EncodedToken);
            return httpClient;
        }

        public async Task<T?> RetrieveAsync(CancellationToken cancellationToken)
        {
            var httpClient = await this.GetHttpClientAsync(cancellationToken);
            if (httpClient == null)
            {
                return default;
            }

            return await httpClient.GetFromJsonAsync<T>(ProfileEndpoint, cancellationToken: cancellationToken)
                .ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        if (t.Exception != null)
                        {
                            logger.LogError(t.Exception, "Error retrieving profile from upstream.");
                            throw new Cod.ApplicationException(Cod.InternalError.InternalServerError, "Error retrieving profile.", t.Exception);
                        }
                    }
                    return t.Result;
                }, cancellationToken);
        }

        public async Task MergeAsync(T profile, CancellationToken cancellationToken)
        {
            var httpClient = await this.GetHttpClientAsync(cancellationToken);
            if (httpClient == null)
            {
                return;
            }

            await httpClient.PutAsJsonAsync(ProfileEndpoint, profile, cancellationToken: cancellationToken)
                .ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        if (t.Exception != null)
                        {
                            logger.LogError(t.Exception, "Error merging profile to upstream.");
                            throw new Cod.ApplicationException(Cod.InternalError.InternalServerError, "Error merging profile.", t.Exception);
                        }
                    }
                }, cancellationToken);
        }
    }

}
