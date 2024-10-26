using Cod.File;
using Cod.File.Blob;
using Cod.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cod.Platform.Blob
{
    public static class DependencyModule
    {
        public static IServiceCollection AddFile(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddCodPlatform();
            return services.AddFile(configuration.Bind);
        }

        public static IServiceCollection AddBlobResourceTokenSupport(this IServiceCollection services)
        {
            services.AddCodPlatform();
            services.AddTransient<ISignatureIssuer, AzureBlobSignatureIssuer>();
            services.AddTransient<IResourceControl, DefaultBlobControl>();
            return services;
        }

        public static IServiceCollection GrantBlobEntitlementTo(
            this IServiceCollection services,
            Func<IServiceProvider, string> resolveRole,
            FilePermissions permissions,
            Func<IServiceProvider, string> resolveContainerName,
            Func<IServiceProvider, string> resolvefullyQualifiedDomainName)
        {
            return services.AddTransient<IEntitlementDescriptor>(sp =>
            {
                var role = resolveRole(sp);
                var container = resolveContainerName(sp);
                var fullyQualifiedDomainName = resolvefullyQualifiedDomainName(sp);
                return new RoleBasedEntitlementDescriptor(role, permissions, fullyQualifiedDomainName, container);
            });
        }

        public static IServiceCollection GrantBlobPersonalizedEntitlementTo(
            this IServiceCollection services,
            Func<IServiceProvider, string> resolveRole,
            FilePermissions permissions,
            Func<IServiceProvider, IEnumerable<string>> resolveContainerNamePrefix,
            Func<IServiceProvider, string> resolvefullyQualifiedDomainName)
        {
            return services.AddTransient<IEntitlementDescriptor>(sp =>
            {
                var role = resolveRole(sp);
                var containerNamePrefix = resolveContainerNamePrefix(sp);
                var fullyQualifiedDomainName = resolvefullyQualifiedDomainName(sp);
                return new PersonalizedEntitlementDescriptor(role, permissions, fullyQualifiedDomainName, containerNamePrefix);
            });
        }

        public static IServiceCollection GrantBlobEntitlementTo(this IServiceCollection services, string role, FilePermissions permissions, string containerName, string fullyQualifiedDomainName)
            => services.GrantBlobEntitlementTo(_ => role, permissions, _ => containerName, _ => fullyQualifiedDomainName);

        public static IServiceCollection GrantBlobPersonalizedEntitlementTo(this IServiceCollection services, string role, FilePermissions permissions, string containerNamePrefix, string fullyQualifiedDomainName)
            => services.GrantBlobPersonalizedEntitlementTo(_ => role, permissions, _ => [containerNamePrefix], _ => fullyQualifiedDomainName);

        public static IServiceCollection GrantBlobPersonalizedEntitlementTo(this IServiceCollection services, string role, FilePermissions permissions, IEnumerable<string> containerNamePrefix, string fullyQualifiedDomainName)
            => services.GrantBlobPersonalizedEntitlementTo(_ => role, permissions, _ => containerNamePrefix, _ => fullyQualifiedDomainName);

        public static IServiceCollection GrantBlobPersonalizedEntitlementTo(
            this IServiceCollection services,
            Func<IServiceProvider, string> resolveRole,
            FilePermissions permissions,
            Func<IServiceProvider, string> resolveContainerNamePrefix,
            Func<IServiceProvider, string> resolvefullyQualifiedDomainName)
            => services.GrantBlobPersonalizedEntitlementTo(resolveRole, permissions, sp => [resolveContainerNamePrefix(sp)], resolvefullyQualifiedDomainName);
    }
}