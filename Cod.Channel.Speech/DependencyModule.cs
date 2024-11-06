using Cod.Channel.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Cod.Channel.Speech
{
    public static class DependencyModule
    {
        private static volatile bool loaded;

        public static IServiceCollection AddSpeech(this IServiceCollection services, Action<IdentityServiceOptions> identityOptions)
        {
            if (loaded)
            {
                return services;
            }

            loaded = true;

            services.AddIdentity(identityOptions);
            services.AddSingleton<SpeechService>();
            services.AddTransient<ISpeechService>(sp => sp.GetRequiredService<SpeechService>());
            services.AddTransient<IDomainEventHandler<ISpeechRecognizer>>(sp => sp.GetRequiredService<SpeechService>());
            services.AddTransient<ICommand<SpeechRecognizeCommandParameter, bool>, SpeechRecognizeCommand>();
            return services;
        }
    }
}