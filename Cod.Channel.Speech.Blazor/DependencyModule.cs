using Cod.Channel.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Cod.Channel.Speech.Blazor
{
    public static class DependencyModule
    {
        private static volatile bool loaded;

        public static IServiceCollection AddSpeechBlazor(this IServiceCollection services, Action<IdentityServiceOptions>? identityOptions = null)
        {
            if (loaded)
            {
                return services;
            }

            loaded = true;

            if (identityOptions != null)
            {
                services.AddSpeech(identityOptions);
            }

            services.AddSingleton<ISpeechRecognizer, JSSpeechRecognizer>();
            return services;
        }
    }
}
