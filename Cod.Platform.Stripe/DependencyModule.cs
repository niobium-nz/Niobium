using Cod.Platform.Finance;
using Microsoft.Extensions.DependencyInjection;

namespace Cod.Platform
{
    public static class DependencyModule
    {
        public static IServiceCollection AddStripePlatform(this IServiceCollection services)
        {
            Cod.InternalError.Register(new InternalErrorRetriever());

            services.AddTransient<StripeIntegration>();
            services.AddTransient<IPaymentProcessor, StripePaymentProcessor>();

            return services;
        }
    }
}
