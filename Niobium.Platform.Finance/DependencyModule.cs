using Microsoft.AspNetCore.Builder;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Niobium.Finance;

namespace Niobium.Platform.Finance
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

        public static IApplicationBuilder UsePlatformPayment<TDepositHandler, TAccountableDomain, TAccountableEntity>(this IApplicationBuilder builder)
            where TDepositHandler : AccountDepositRecorder<TAccountableDomain, TAccountableEntity>
            where TAccountableDomain : AccountableDomain<TAccountableEntity>
            where TAccountableEntity : class, new()
        {
            builder.UsePlatform();
            builder.UseMiddleware<PaymentRequestMiddleware>();
            builder.UseMiddleware<PaymentWebhookMiddleware>();

            return builder;
        }
    }
}