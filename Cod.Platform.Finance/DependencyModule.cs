using Microsoft.Extensions.DependencyInjection;

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
            return services;
        }
    }
}