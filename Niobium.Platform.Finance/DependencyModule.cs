using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Niobium.Platform.Finance
{
    public static class DependencyModule
    {
        private static volatile bool loaded;

        public static IServiceCollection AddFinance(this IServiceCollection services, Action<PaymentServiceOptions> options)
        {
            if (loaded)
            {
                return services;
            }

            loaded = true;

            services.Configure<PaymentServiceOptions>(o => options(o));

            services.AddPlatform();
            services.AddTransient<IPaymentService, PaymentService>();
            services.AddTransient<PaymentRequestMiddleware>();
            services.AddTransient<PaymentWebhookMiddleware>();
            return services;
        }

        public static IApplicationBuilder UsePlatformPayment(this IApplicationBuilder builder)
        {
            builder.UsePlatform();
            builder.UseMiddleware<PaymentRequestMiddleware>();
            builder.UseMiddleware<PaymentWebhookMiddleware>();

            return builder;
        }
    }
}