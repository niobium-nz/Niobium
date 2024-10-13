using Cod.Identity;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace Cod.Channel.Identity.Blazor
{
    internal class LocalStorageAuthenticator(
        IOptions<IdentityServiceOptions> options,
        HttpClient httpClient,
        Lazy<IEnumerable<IDomainEventHandler<IAuthenticator>>> eventHandlers,
        IJSRuntime jsRuntime)
            : DefaultAuthenticator(options, httpClient, eventHandlers), IAsyncDisposable
    {
        private readonly Lazy<ValueTask<IJSObjectReference>> localStorageModule
            = new(() => jsRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/Cod.Channel.Identity.Blazor/authenticator.js"));

        private const string JSInteropGet = "getCache";
        private const string JSInteropSet = "setCache";
        private const string AccessTokenCacheKey = "accessToken";
        private const string IDTokenCacheKey = "idToken";
        private const string ResourceTokensCacheKey = "resourceTokens";
        private static readonly Dictionary<string, StorageSignature> EmptySignatures = [];

        protected override async Task<string?> GetSavedAccessTokenAsync()
        {
            var js = await localStorageModule.Value;
            return await js.InvokeAsync<string>(JSInteropGet, AccessTokenCacheKey);
        }

        protected override async Task SaveAccessTokenAsync(string? token)
        {
            var js = await localStorageModule.Value;
            await js.InvokeVoidAsync(JSInteropSet, AccessTokenCacheKey, token ?? string.Empty);
        }

        protected override async Task<string?> GetSavedIDTokenAsync()
        {
            var js = await localStorageModule.Value;
            return await js.InvokeAsync<string>(JSInteropGet, IDTokenCacheKey);
        }

        protected override async Task SaveIDTokenAsync(string? token)
        {
            var js = await localStorageModule.Value;
            await js.InvokeVoidAsync(JSInteropSet, IDTokenCacheKey, token ?? string.Empty);
        }

        protected override async Task<IDictionary<string, StorageSignature>> GetSavedResourceTokensAsync()
        {
            var js = await localStorageModule.Value;
            var json = await js.InvokeAsync<string>(JSInteropGet, ResourceTokensCacheKey);
            if (!string.IsNullOrWhiteSpace(json))
            {
                return JsonSerializer.DeserializeObject<Dictionary<string, StorageSignature>>(json);
            }
            else
            {
                return EmptySignatures;
            }
        }

        protected override async Task SaveResourceTokensAsync(IDictionary<string, StorageSignature> resourceTokens)
        {
            var js = await localStorageModule.Value;
            await js.InvokeVoidAsync(JSInteropSet, ResourceTokensCacheKey, JsonSerializer.SerializeObject(resourceTokens));
        }

        protected override async ValueTask DisposeAsync(bool disposing)
        {
            if (disposing)
            {
                if (localStorageModule.IsValueCreated)
                {
                    var module = await localStorageModule.Value;
                    await module.DisposeAsync();
                }
            }

            await base.DisposeAsync(disposing);
        }
    }
}
