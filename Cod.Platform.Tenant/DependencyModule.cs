using Cod.Platform.Identity;
using Cod.Platform.Tenant.Wechat;
using Cod.Storage.Table;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cod.Platform.Tenant
{
    public static class DependencyModule
    {
        private static volatile bool loaded;

        public static IServiceCollection AddPlatformTenant(this IServiceCollection services, IConfiguration configuration)
        {
            if (loaded)
            {
                return services;
            }

            loaded = true;

            services.AddStorageTable(configuration);
            services.AddCodPlatform();

            services.AddTransient<WechatIntegration>();
            services.AddTransient<IBusinessManager, MemoryCachedBusinessManager>();
            services.AddTransient<IBrandService, MemoryCachedBrandService>();
            services.AddTransient<IOpenIDManager, OpenIDManager>();

            services.AddTransient<IQueryableRepository<OpenID>, CloudTableRepository<OpenID>>();
            services.AddTransient<IRepository<OpenID>, CloudTableRepository<OpenID>>();
            services.AddTransient<IQueryableRepository<Business>, CloudTableRepository<Business>>();
            services.AddTransient<IRepository<Business>, CloudTableRepository<Business>>();
            services.AddTransient<IQueryableRepository<BrandingInfo>, CloudTableRepository<BrandingInfo>>();
            services.AddTransient<IRepository<BrandingInfo>, CloudTableRepository<BrandingInfo>>();

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