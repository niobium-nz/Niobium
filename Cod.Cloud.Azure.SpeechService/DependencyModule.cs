using Cod.Platform;
using Microsoft.Extensions.DependencyInjection;

namespace Cod.Cloud.Azure.SpeechService
{
    public static class DependencyModule
    {
        private static volatile bool loaded;

        public static IServiceCollection AddSpeechService(this IServiceCollection services, SpeechServiceOptions options)
        {
            if (loaded)
            {
                return services;
            }

            loaded = true;

            options.Validate();
            services.AddSingleton(options);

            services.AddCodPlatform();

            services.AddTransient<IResourceControl, SpeechServiceControl>();
            services.AddHttpClient<ISignatureIssuer, SpeechServiceSignatureIssuer>((serviceProvider, httpClient) =>
            {
                var options = serviceProvider.GetRequiredService<SpeechServiceOptions>();
                httpClient.BaseAddress = new Uri($"https://{options.ServiceRegion}.api.cognitive.microsoft.com/");
                httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", options.AccessKey);
            });
            return services;
        }
    }
}
