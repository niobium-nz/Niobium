using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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

            Niobium.Platform.Finance.DependencyModule.AddFinance(services, options);

            Niobium.InternalError.Register(new InternalErrorRetriever());

            services.AddSingleton<ServiceManager>();
            services.AddTransient<StripeIntegration>();
            services.AddTransient<IPaymentProcessor, StripePaymentProcessor>();

            return services;
        }
    }
}
