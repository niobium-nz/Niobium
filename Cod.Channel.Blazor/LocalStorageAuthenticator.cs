using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cod;
using Microsoft.JSInterop;
using Newtonsoft.Json;

namespace Cod.Channel.Blazor
{
    internal class LocalStorageAuthenticator : DefaultAuthenticator
    {
        private const string JSInteropGet = "getCache";
        private const string JSInteropSet = "setCache";
        private const string TokenCacheKey = "accessToken";
        private const string SignatureCacheKey = "signatures";
        private static readonly Dictionary<string, StorageSignature> EmptySignatures = new Dictionary<string, StorageSignature>();
        private readonly IJSRuntime runtime;

        public LocalStorageAuthenticator(
            IConfigurationProvider configuration,
            Lazy<IHttpClient> httpClient,
            IEnumerable<IEventHandler<IAuthenticator>> eventHandlers,
            IJSRuntime runtime)
            : base(configuration, httpClient, eventHandlers) => this.runtime = runtime;

        protected override async Task SaveTokenAsync(string token) => await this.runtime.InvokeVoidAsync(JSInteropSet, TokenCacheKey, token);

        protected override async Task<string> GetSavedTokenAsync() => await this.runtime.InvokeAsync<string>(JSInteropGet, TokenCacheKey);

        protected override async Task<IDictionary<string, StorageSignature>> GetSavedSignaturesAsync()
        {
            var sig = await this.runtime.InvokeAsync<string>(JSInteropGet, SignatureCacheKey);
            if (!String.IsNullOrWhiteSpace(sig))
            {
                return JsonConvert.DeserializeObject<Dictionary<string, StorageSignature>>(sig);
            }
            else
            {
                return EmptySignatures;
            }
        }

        protected override async Task SaveSignaturesAsync(IDictionary<string, StorageSignature> signatures) => await this.runtime.InvokeVoidAsync(JSInteropSet, SignatureCacheKey, JsonConvert.SerializeObject(signatures));
    }
}
