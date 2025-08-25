using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Stripe;

namespace Niobium.Platform.Finance.Stripe
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

            services.AddFinance();

            Niobium.InternalError.Register(new InternalErrorRetriever());

            services.Configure<PaymentServiceOptions>(o =>
            {
                options(o);
                StripeConfiguration.ApiKey = o.SecretAPIKey;
            });

            services.AddTransient<StripeIntegration>();
            services.AddTransient<IPaymentProcessor, StripePaymentProcessor>();

            return services;
        }
    }
}
