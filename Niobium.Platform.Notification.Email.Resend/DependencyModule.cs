using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Niobium.Platform.Notification.Email.Resend
{
    public static class DependencyModule
    {
        private static volatile bool loaded;

        public static void AddNotification(this IHostApplicationBuilder builder)
        {
            builder.Services.AddNotification(builder.Configuration.GetSection(nameof(ResendServiceOptions)).Bind);


            if (builder.Configuration.IsPreProductionEnvironment())
            {
                builder.Services.AddTransient<IEmailNotificationClient, DevelopmentEmailNotificationClient>();
            }
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
                httpClient.BaseAddress = new Uri("https://api.resend.com/");
            })
            .AddStandardResilienceHandler();

            return services;
        }
    }
}