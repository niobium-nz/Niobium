using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text;

namespace Cod.Profile
{
    public abstract class GenericProfileService<T>(
        IHttpClientFactory httpClientFactory,
        IOptions<ProfileOptions> options,
        ILogger<GenericProfileService<T>> logger)
        : IProfileService<T> where T : class, IProfile
    {
        private T? profile;

        public virtual string ProfileEndpoint { get => options.Value.ProfileServiceEndpoint; }

        public async Task<T?> RetrieveAsync(bool forceRefresh = false, CancellationToken? cancellationToken = null)
        {
            cancellationToken ??= CancellationToken.None;

            if (!forceRefresh && profile != null)
            {
                return profile;
            }

            var httpClient = await this.GetHttpClientAsync(cancellationToken.Value);
            if (httpClient == null)
            {
                return null;
            }

            return await httpClient.GetFromJsonAsync<T>(ProfileEndpoint, cancellationToken: cancellationToken.Value)
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
                }, cancellationToken.Value);
        }

        public async Task MergeAsync(T profile, CancellationToken? cancellationToken = null)
        {
            cancellationToken ??= CancellationToken.None;

            var httpClient = await this.GetHttpClientAsync(cancellationToken.Value);
            if (httpClient == null)
            {
                return;
            }

            var json = System.Text.Json.JsonSerializer.Serialize(profile);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            await httpClient.PutAsync(ProfileEndpoint, content, cancellationToken: cancellationToken.Value)
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
                }, cancellationToken.Value);

            this.profile = default;
        }

        protected virtual async Task<HttpClient?> GetHttpClientAsync(CancellationToken cancellationToken)
        {
            var httpClient = httpClientFactory.CreateClient(Cod.Profile.Constants.DefaultHttpClientName);
            return await ConfigureHttpClientAsync(httpClient, cancellationToken);
        }

        protected abstract Task<HttpClient?> ConfigureHttpClientAsync(HttpClient httpClient, CancellationToken cancellationToken);
    }
}
