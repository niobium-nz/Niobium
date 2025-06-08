using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;

namespace Cod.Platform.Notification.Email.Resend
{
    public static class DependencyModule
    {
        private const string resendAPIHost = "https://api.resend.com/";
        private static volatile bool loaded;

        public static void AddNotification(this IHostApplicationBuilder builder)
        {
            builder.Services.AddNotification(builder.Configuration.GetSection(nameof(ResendServiceOptions)).Bind);
        }

        public static IServiceCollection AddNotification(
            this IServiceCollection services,
            Action<ResendServiceOptions>? options)
        {
            if (loaded)
            {
                return services;
            }

            loaded = true;

            services.AddPlatform();

            services.Configure<ResendServiceOptions>(o => { options?.Invoke(o); o.Validate(); });

            services.AddHttpClient<IEmailNotificationClient, ResendEmailNotificationClient>((serviceProvider, httpClient) =>
            {
                var config = serviceProvider.GetRequiredService<IOptions<ResendServiceOptions>>();
                httpClient.BaseAddress = new Uri(resendAPIHost);
                if (!string.IsNullOrWhiteSpace(config.Value.GlobalAPIKey))
                {
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", config.Value.GlobalAPIKey);
                }
            })
            .AddStandardResilienceHandler();

            return services;
        }
    }
}