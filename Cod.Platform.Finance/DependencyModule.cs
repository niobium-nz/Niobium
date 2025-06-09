using Microsoft.AspNetCore.Builder;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cod.Platform.Finance
{
    public static class DependencyModule
    {
        private static volatile bool loaded;

        public static IServiceCollection AddFinance(this IServiceCollection services)
        {
            if (loaded)
            {
                return services;
            }

            loaded = true;

            services.AddPlatform();
            services.AddTransient<IPaymentService, PaymentService>();
            services.AddTransient<PaymentRequestMiddleware>();
            services.AddTransient<PaymentWebhookMiddleware>();
            return services;
        }

        public static IFunctionsWorkerApplicationBuilder UsePlatformPayment(this IFunctionsWorkerApplicationBuilder builder)
        {
            builder.UsePlatform();
            builder.UseWhen<FunctionMiddlewareAdaptor<PaymentRequestMiddleware>>(FunctionMiddlewarePredicates.IsHttp);
            builder.UseWhen<FunctionMiddlewareAdaptor<PaymentWebhookMiddleware>>(FunctionMiddlewarePredicates.IsHttp);
            return builder;
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