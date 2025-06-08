using Microsoft.AspNetCore.Builder;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Stripe;

namespace Cod.Platform.Finance.Stripe
{
    public static class DependencyModule
    {
        private static volatile bool loaded;

        public static void AddFinance(this IHostApplicationBuilder builder)
        {
            builder.Services.AddFinance(builder.Configuration.GetSection(nameof(PaymentServiceOptions)).Bind);
        }

        public static IServiceCollection AddFinance(this IServiceCollection services, Action<PaymentServiceOptions> options)
        {
            if (loaded)
            {
                return services;
            }

            loaded = true;

            Cod.Platform.Finance.DependencyModule.AddFinance(services);

            Cod.InternalError.Register(new InternalErrorRetriever());

            services.Configure<PaymentServiceOptions>(o =>
            {
                options(o);
                StripeConfiguration.ApiKey = o.SecretAPIKey;
            });

            services.AddTransient<StripeIntegration>();
            services.AddTransient<IPaymentProcessor, StripePaymentProcessor>();
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
