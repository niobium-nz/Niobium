using Cod.Platform.Finance;
using Cod.Platform.Identity.Authentication;
using Cod.Platform.Identity.Authorization;
using Cod.Platform.Locking;
using Cod.Platform.Tenant;
using Cod.Storage.Table;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cod.Platform.Identity
{
    public static class DependencyModule
    {
        private static volatile bool loaded;

        public static IServiceCollection AddPlatformIdentity(this IServiceCollection services, IConfiguration configuration)
        {
            if (loaded)
            {
                return services;
            }

            loaded = true;

            services.AddStorageTable(configuration);
            services.AddCodPlatform();
            services.AddPlatformFinance(configuration);
            services.AddPlatformLocking(configuration);

            services.AddTransient<IOpenIDManager, OpenIDManager>();
            services.AddTransient<ISignatureService, AzureStorageSignatureService>();
            services.AddTransient<ITokenBuilder, BearerTokenBuilder>();
            services.AddTransient<IQueryableRepository<OpenID>, CloudTableRepository<OpenID>>();
            services.AddTransient<IRepository<OpenID>, CloudTableRepository<OpenID>>();

            services.AddTransient<IQueryableRepository<Login>, CloudTableRepository<Login>>();
            services.AddTransient<IRepository<Login>, CloudTableRepository<Login>>();

            services.AddTransient<IQueryableRepository<User>, CloudTableRepository<User>>();
            services.AddTransient<IRepository<User>, CloudTableRepository<User>>();
            services.AddTransient<UserDomain>();
            services.AddTransient<Func<UserDomain>>(sp => () => sp.GetService<UserDomain>());
            services.AddTransient<IDomainRepository<UserDomain, User>, GenericDomainRepository<UserDomain, User>>();

            return services;
        }
    }
}