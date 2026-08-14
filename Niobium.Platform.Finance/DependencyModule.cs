using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Niobium.Platform.Finance
{
    public static class DependencyModule
    {
        private static volatile bool middlewareRegistered;
        private static volatile bool added;
        private static volatile bool used;

        public static IServiceCollection AddFinance(this IServiceCollection services, Action<PaymentServiceOptions> options)
        {
            if (added)
            {
                return services;
            }

            added = true;

            services.Configure<PaymentServiceOptions>(o => options(o));

            services.AddPlatform();
            services.AddTransient<IPaymentService, PaymentService>();
            services.AddTransient<PaymentRequestMiddleware>();
            services.AddTransient<PaymentWebhookMiddleware>();
            return services;
        }

        public static IApplicationBuilder UsePlatformPayment(this IApplicationBuilder builder)
        {
            if (used)
            {
                return builder;
            }

            used = true;

            builder.UsePlatform();
            builder.ToMiddlewareHost().UsePlatformPayment();
            return builder;
        }

        public static IMiddlewareHost UsePlatformPayment(this IMiddlewareHost builder)
        {
            if (middlewareRegistered)
            {
                return builder;
            }

            middlewareRegistered = true;

            builder.UsePlatform();
            builder.UseMiddleware<PaymentRequestMiddleware>();
            builder.UseMiddleware<PaymentWebhookMiddleware>();
            return builder;
        }
    }
}