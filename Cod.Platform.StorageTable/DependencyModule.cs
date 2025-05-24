using Cod.Database.StorageTable;
using Cod.Identity;
using Cod.Platform.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cod.Platform.StorageTable
{
    public static class DependencyModule
    {
        public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddPlatform();

            services.AddTransient(typeof(IQueryableRepository<>), typeof(QueryableCloudTableRepository<>));
            return services.AddDatabase(configuration.Bind);
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
        {
            return services.AddTransient<IEntitlementDescriptor>(sp =>
            {
                var role = resolveRole(sp);
                var table = resolveTableName(sp);
                var fullyQualifiedDomainName = resolveFullyQualifiedDomainName(sp);
                return new RoleBasedEntitlementDescriptor(role, permissions, fullyQualifiedDomainName, table);
            });
        }

        public static IServiceCollection GrantDatabasePersonalizedEntitlementTo(
            this IServiceCollection services,
            Func<IServiceProvider, string> resolveRole,
            DatabasePermissions permissions,
            Func<IServiceProvider, string> resolveTableName,
            Func<IServiceProvider, string> resolveFullyQualifiedDomainName)
        {
            return services.AddTransient<IEntitlementDescriptor>(sp =>
            {
                var role = resolveRole(sp);
                var table = resolveTableName(sp);
                var fullyQualifiedDomainName = resolveFullyQualifiedDomainName(sp);
                return new PersonalizedEntitlementDescriptor(role, permissions, fullyQualifiedDomainName, table);
            });
        }

        public static IServiceCollection AddDatabaseResourceTokenSupport(this IServiceCollection services, IConfiguration identityConfiguration)
            => services.AddDatabaseResourceTokenSupport(identityConfiguration.Bind);

        public static IServiceCollection GrantDatabaseEntitlementTo(this IServiceCollection services, string role, DatabasePermissions permissions, string tableName, string fullyQualifiedDomainName)
            => services.GrantDatabaseEntitlementTo(_ => role, permissions, _ => tableName, _ => fullyQualifiedDomainName);

        public static IServiceCollection GrantDatabasePersonalizedEntitlementTo(this IServiceCollection services, string role, DatabasePermissions permissions, string tableName, string fullyQualifiedDomainName)
            => services.GrantDatabasePersonalizedEntitlementTo(_ => role, permissions, _ => tableName, _ => fullyQualifiedDomainName);
    }
}