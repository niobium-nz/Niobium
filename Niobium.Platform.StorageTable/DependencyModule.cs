using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Niobium.Database.StorageTable;
using Niobium.Identity;
using Niobium.Platform.Identity;

namespace Niobium.Platform.StorageTable
{
    public static class DependencyModule
    {
        private static volatile bool loaded;

        public static void AddDatabase(this IHostApplicationBuilder builder)
        {
            builder.Services.AddDatabase(builder.Configuration.GetSection(nameof(StorageTableOptions)).Bind);

            bool isDevelopment = builder.Configuration.IsDevelopmentEnvironment();
            if (isDevelopment)
            {
                builder.Services.PostConfigure<StorageTableOptions>(opt => opt.EnableInteractiveIdentity = true);
            }
        }

        public static IServiceCollection AddDatabase(this IServiceCollection services, Action<StorageTableOptions> options)
        {
            if (loaded)
            {
                return services;
            }

            loaded = true;

            services.AddPlatform();

            services.AddTransient(typeof(IQueryableRepository<>), typeof(QueryableCloudTableRepository<>));
            Database.StorageTable.DependencyModule.AddDatabase(services, options);

            return services;
        }

        public static IServiceCollection AddDatabaseResourceTokenSupport(this IServiceCollection services, Action<IdentityServiceOptions> options)
        {
            services.AddIdentity(options);
            services.AddTransient<ISignatureIssuer, AzureTableSignatureIssuer>();
            services.AddResourceControl<DefaultTableControl>();
            return services;
        }

        public static IServiceCollection GrantDatabaseEntitlementTo(
            this IServiceCollection services,
            Func<IServiceProvider, string> resolveRole,
            DatabasePermissions permissions,
            Func<IServiceProvider, string> resolveTableName,
            Func<IServiceProvider, string> resolveFullyQualifiedDomainName)
            => services.AddTransient<IEntitlementDescriptor>(sp =>
            {
                string role = resolveRole(sp);
                string table = resolveTableName(sp);
                string fullyQualifiedDomainName = resolveFullyQualifiedDomainName(sp);
                return new RoleBasedEntitlementDescriptor(role, permissions, fullyQualifiedDomainName, table);
            });

        public static IServiceCollection GrantDatabasePersonalizedEntitlementTo(
            this IServiceCollection services,
            Func<IServiceProvider, string> resolveRole,
            DatabasePermissions permissions,
            Func<IServiceProvider, string> resolveTableName,
            Func<IServiceProvider, string> resolveFullyQualifiedDomainName)
            => services.AddTransient<IEntitlementDescriptor>(sp =>
            {
                string role = resolveRole(sp);
                string table = resolveTableName(sp);
                string fullyQualifiedDomainName = resolveFullyQualifiedDomainName(sp);
                return new PersonalizedEntitlementDescriptor(role, permissions, fullyQualifiedDomainName, table);
            });

        public static IServiceCollection AddDatabaseResourceTokenSupport(this IHostApplicationBuilder builder)
            => builder.Services.AddDatabaseResourceTokenSupport(builder.Configuration.GetSection(nameof(IdentityServiceOptions)).Bind);

        public static IServiceCollection GrantDatabaseEntitlementTo(this IServiceCollection services, string role, DatabasePermissions permissions, string tableName, string fullyQualifiedDomainName)
            => services.GrantDatabaseEntitlementTo(_ => role, permissions, _ => tableName, _ => fullyQualifiedDomainName);

        public static IServiceCollection GrantDatabasePersonalizedEntitlementTo(this IServiceCollection services, string role, DatabasePermissions permissions, string tableName, string fullyQualifiedDomainName)
            => services.GrantDatabasePersonalizedEntitlementTo(_ => role, permissions, _ => tableName, _ => fullyQualifiedDomainName);

        public static IServiceCollection GrantDatabasePersonalizedEntitlementTo(this IServiceCollection services, string table, DatabasePermissions permissions = DatabasePermissions.Query)
            => services.GrantDatabasePersonalizedEntitlementTo(
                sp => sp.GetRequiredService<IOptions<IdentityServiceOptions>>().Value.DefaultRole,
                permissions,
                sp => table,
                GetFullyQualifiedDomainName);

        public static IServiceCollection GrantDatabaseEntitlementTo(this IServiceCollection services, string table, DatabasePermissions permissions = DatabasePermissions.Query)
            => services.GrantDatabaseEntitlementTo(
                sp => sp.GetRequiredService<IOptions<IdentityServiceOptions>>().Value.DefaultRole,
                permissions,
                sp => table,
                GetFullyQualifiedDomainName);

        private static string GetFullyQualifiedDomainName(IServiceProvider sp)
        {
            IOptions<StorageTableOptions> option = sp.GetRequiredService<IOptions<StorageTableOptions>>();
            string? fdqn = option.Value.FullyQualifiedDomainName;
            if (fdqn != null)
            {
                IConfiguration config = sp.GetRequiredService<IConfiguration>();
                string? uri = config[Database.StorageTable.Constants.DefaultTableServiceUriSetting];
                if (!Uri.TryCreate(uri, UriKind.Absolute, out Uri? parsedUri))
                {
                    throw new InvalidOperationException($"Invalid URI for table service: {uri}");
                }
                fdqn = parsedUri.Host;
            }
            return fdqn ?? throw new InvalidOperationException("Fully qualified domain name is not available");
        }
    }
}