using Cod.Platform.Finance.WechatPay;
using Cod.Platform.Tenant;
using Cod.Storage.Table;
using Microsoft.Extensions.DependencyInjection;

namespace Cod.Platform.Finance
{
    public static class DependencyModule
    {
        private static volatile bool loaded;

        public static IServiceCollection AddPlatformFinance(this IServiceCollection services, StorageTableOptions options)
        {
            if (loaded)
            {
                return services;
            }

            loaded = true;

            services.AddStorageTable(options);
            services.AddCodPlatform();
            services.AddPlatformTenant(options);

            services.AddTransient<IPaymentService, PaymentService>();
            services.AddTransient<IPaymentProcessor, WechatPaymentProcessor>();

            services.AddTransient<IQueryableRepository<Accounting>, CloudTableRepository<Accounting>>();
            services.AddTransient<IRepository<Accounting>, CloudTableRepository<Accounting>>();

            services.AddTransient<IQueryableRepository<Transaction>, CloudTableRepository<Transaction>>();
            services.AddTransient<IRepository<Transaction>, CloudTableRepository<Transaction>>();

            services.AddTransient<IQueryableRepository<PaymentMethod>, CloudTableRepository<PaymentMethod>>();
            services.AddTransient<IRepository<PaymentMethod>, CloudTableRepository<PaymentMethod>>();

            return services;
        }
    }
}