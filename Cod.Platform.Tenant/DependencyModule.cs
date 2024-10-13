using Cod.Platform.Identity;
using Cod.Platform.Tenant.Wechat;
using Cod.Table;
using Microsoft.Extensions.DependencyInjection;

namespace Cod.Platform.Tenant
{
    public static class DependencyModule
    {
        private static volatile bool loaded;

        public static IServiceCollection AddPlatformTenant(this IServiceCollection services)
        {
            if (loaded)
            {
                return services;
            }

            loaded = true;

            services.AddCodPlatform();

            services.AddTransient<WechatIntegration>();
            services.AddTransient<IBusinessManager, MemoryCachedBusinessManager>();
            services.AddTransient<IBrandService, MemoryCachedBrandService>();
            services.AddTransient<IOpenIDManager, OpenIDManager>();
            services.AddTransient<WechatRepository>();
            services.AddTransient<IRepository<WechatEntity>>(sp =>
                new CachedRepository<WechatEntity>(
                    sp.GetService<WechatRepository>(),
                    sp.GetService<ICacheStore>(),
                    true
                ));

            return services;
        }
    }
}