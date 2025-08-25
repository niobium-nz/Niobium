using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using Niobium.Identity;

namespace Niobium.Channel.Identity.Blazor
{
    internal sealed class LocalStorageAuthenticator(
        IOptions<IdentityServiceOptions> options,
        IdentityService identityService,
        Lazy<IEnumerable<IDomainEventHandler<IAuthenticator>>> eventHandlers,
        IJSRuntime jsRuntime)
            : DefaultAuthenticator(options, identityService, eventHandlers), IAsyncDisposable
    {
        private readonly Lazy<ValueTask<IJSObjectReference>> localStorageModule
            = new(() => jsRuntime.InvokeAsync<IJSObjectReference>("import", $"./_content/{typeof(LocalStorageAuthenticator).Assembly.FullName!.Split(',')[0].Trim()}/authenticator.js"));

        private const string JSInteropGet = "getCache";
        private const string JSInteropSet = "setCache";
        private const string AccessTokenCacheKey = "accessToken";
        private const string IDTokenCacheKey = "idToken";
        private const string ResourceTokensCacheKey = "resourceTokens";
        private static readonly Dictionary<string, StorageSignature> EmptySignatures = [];

        protected override async Task<string?> GetSavedAccessTokenAsync()
        {
            IJSObjectReference js = await localStorageModule.Value;
            return await js.InvokeAsync<string>(JSInteropGet, AccessTokenCacheKey);
        }

        protected override async Task SaveAccessTokenAsync(string? token)
        {
            IJSObjectReference js = await localStorageModule.Value;
            await js.InvokeVoidAsync(JSInteropSet, AccessTokenCacheKey, token ?? string.Empty);
        }

        protected override async Task<string?> GetSavedIDTokenAsync()
        {
            IJSObjectReference js = await localStorageModule.Value;
            return await js.InvokeAsync<string>(JSInteropGet, IDTokenCacheKey);
        }

        protected override async Task SaveIDTokenAsync(string? token)
        {
            IJSObjectReference js = await localStorageModule.Value;
            await js.InvokeVoidAsync(JSInteropSet, IDTokenCacheKey, token ?? string.Empty);
        }

        protected override async Task<IDictionary<string, StorageSignature>> GetSavedResourceTokensAsync()
        {
            IJSObjectReference js = await localStorageModule.Value;
            string json = await js.InvokeAsync<string>(JSInteropGet, ResourceTokensCacheKey);
            return !string.IsNullOrWhiteSpace(json)
                ? JsonSerializer.DeserializeObject<Dictionary<string, StorageSignature>>(json)
                : EmptySignatures;
        }

        protected override async Task SaveResourceTokensAsync(IDictionary<string, StorageSignature> resourceTokens)
        {
            IJSObjectReference js = await localStorageModule.Value;
            await js.InvokeVoidAsync(JSInteropSet, ResourceTokensCacheKey, JsonSerializer.SerializeObject(resourceTokens));
        }

        protected override async ValueTask DisposeAsync(bool disposing)
        {
            if (disposing)
            {
                if (localStorageModule.IsValueCreated)
                {
                    IJSObjectReference module = await localStorageModule.Value;
                    await module.DisposeAsync();
                }
            }

            await base.DisposeAsync(disposing);
        }
    }
}
