using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Niobium.Platform.OpenAI
{
    public static class DependencyModule
    {
        private static volatile bool loaded;

        public static void AddOpenAI(this IHostApplicationBuilder builder)
        {
            builder.Services.AddOpenAI(builder.Configuration.GetSection(nameof(OpenAIServiceOptions)).Bind);
        }

        public static IServiceCollection AddOpenAI(
            this IServiceCollection services,
            Action<OpenAIServiceOptions>? options)
        {
            if (loaded)
            {
                return services;
            }

            loaded = true;

            services.AddPlatform();

            services.Configure<OpenAIServiceOptions>(o => { options?.Invoke(o); o.Validate(); });

            services.AddHttpClient<IOpenAIService, OpenAIService>((serviceProvider, httpClient) =>
            {
                IOptions<OpenAIServiceOptions> config = serviceProvider.GetRequiredService<IOptions<OpenAIServiceOptions>>();
                httpClient.BaseAddress = new Uri(config.Value.Endpoint);
                httpClient.DefaultRequestHeaders.Add("api-key", config.Value.Secret);
            })
            .AddStandardResilienceHandler();
            return services;
        }
    }
}