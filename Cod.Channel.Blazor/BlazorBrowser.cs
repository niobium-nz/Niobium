using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Cod.Channel.Blazor
{
    internal class BlazorBrowser : IBrowser
    {
        private readonly IJSRuntime runtime;

        public BlazorBrowser(IJSRuntime runtime)
        {
            this.runtime = runtime;
        }

        public async Task<string> GetLocateAsync() => await this.runtime.InvokeAsync<string>("getLocate", null);

        public async Task<string> GetUserAgentAsync() => await this.runtime.InvokeAsync<string>("getUserAgent", null);
    }
}
