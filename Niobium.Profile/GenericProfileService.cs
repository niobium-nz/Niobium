using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text;

namespace Niobium.Profile
{
    public abstract class GenericProfileService<T>(
        IHttpClientFactory httpClientFactory,
        IOptions<ProfileOptions> options,
        ILogger<GenericProfileService<T>> logger)
        : IProfileService<T> where T : class, IProfile
    {
        private T? profile;

        public virtual string ProfileEndpoint => options.Value.ProfileServiceEndpoint;

        protected IOptions<ProfileOptions> Options { get => options; }

        public async Task<T?> RetrieveAsync(Guid tenant, Guid user, bool forceRefresh = false, CancellationToken? cancellationToken = null)
        {
            cancellationToken ??= CancellationToken.None;

            if (!forceRefresh && profile != null)
            {
                return profile;
            }

            HttpClient? httpClient = await GetHttpClientAsync(cancellationToken.Value);
            return httpClient == null
                ? null
                : await httpClient.GetFromJsonAsync<T>($"{ProfileEndpoint}/{tenant}/{user}", cancellationToken: cancellationToken.Value)
                .ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        if (t.Exception != null)
                        {
                            logger.LogError(t.Exception, "Error retrieving profile from upstream.");
                            throw new ApplicationException(InternalError.InternalServerError, "Error retrieving profile.", t.Exception);
                        }
                    }
                    return t.Result;
                }, cancellationToken.Value);
        }

        public async Task MergeAsync(Guid tenant, Guid user, T profile, CancellationToken? cancellationToken = null)
        {
            cancellationToken ??= CancellationToken.None;

            HttpClient? httpClient = await GetHttpClientAsync(cancellationToken.Value);
            if (httpClient == null)
            {
                return;
            }

            string json = JsonMarshaller.Marshall(profile);
            using StringContent content = new(json, Encoding.UTF8, "application/json");
            await httpClient.PutAsync($"{ProfileEndpoint}/{tenant}/{user}", content, cancellationToken: cancellationToken.Value)
                .ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        if (t.Exception != null)
                        {
                            logger.LogError(t.Exception, "Error merging profile to upstream.");
                            throw new ApplicationException(InternalError.InternalServerError, "Error merging profile.", t.Exception);
                        }
                    }
                }, cancellationToken.Value);

            this.profile = default;
        }

        protected virtual async Task<HttpClient?> GetHttpClientAsync(CancellationToken cancellationToken)
        {
            HttpClient httpClient = httpClientFactory.CreateClient(Constants.DefaultHttpClientName);
            return await ConfigureHttpClientAsync(httpClient, cancellationToken);
        }

        protected abstract Task<HttpClient?> ConfigureHttpClientAsync(HttpClient httpClient, CancellationToken cancellationToken);
    }
}
