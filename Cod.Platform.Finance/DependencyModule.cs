using Cod.Platform.Finance.WechatPay;
using Cod.Platform.Tenant;
using Microsoft.Extensions.DependencyInjection;

namespace Cod.Platform.Finance
{
    public static class DependencyModule
    {
        private static volatile bool loaded;

        public static IServiceCollection AddPlatformFinance(this IServiceCollection services)
        {
            if (loaded)
            {
                return services;
            }

            loaded = true;

            services.AddCodPlatform();
            services.AddPlatformTenant();

            services.AddTransient<IPaymentService, PaymentService>();
            services.AddTransient<IPaymentProcessor, WechatPaymentProcessor>();

            return services;
        }
    }
}