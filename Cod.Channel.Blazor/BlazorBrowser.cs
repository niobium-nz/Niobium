using Microsoft.JSInterop;

namespace Cod.Channel.Blazor
{
    internal sealed class BlazorBrowser(IJSRuntime jsRuntime) : IBrowser, IAsyncDisposable
    {
        private readonly Lazy<Task<IJSObjectReference>> browserSupportModule = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
                "import", "./_content/Cod.Channel.Blazor/browserSupport.js").AsTask());

        public async Task<string> GetLocateAsync()
        {
            IJSObjectReference module = await browserSupportModule.Value;
            return await module.InvokeAsync<string>("getLocate", null);
        }

        public async Task<string> GetUserAgentAsync()
        {
            IJSObjectReference module = await browserSupportModule.Value;
            return await module.InvokeAsync<string>("getUserAgent", null);
        }

        public async ValueTask DisposeAsync()
        {
            if (browserSupportModule.IsValueCreated)
            {
                IJSObjectReference module = await browserSupportModule.Value;
                await module.DisposeAsync();
            }
        }
    }
}
