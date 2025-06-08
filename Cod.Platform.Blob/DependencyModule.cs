using Cod.File;
using Cod.File.Blob;
using Cod.Identity;
using Cod.Platform.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Cod.Platform.Blob
{
    public static class DependencyModule
    {
        private static volatile bool loaded;

        public static void AddFile(this IHostApplicationBuilder builder)
        {
            builder.Services.AddFile(builder.Configuration.GetSection(nameof(StorageBlobOptions)).Bind);
        }

        public static IServiceCollection AddFile(this IServiceCollection services, Action<StorageBlobOptions>? options = null)
        {
            if (loaded)
            {
                return services;
            }

            loaded = true;
            services.AddPlatform();

            return Cod.File.Blob.DependencyModule.AddFile(services, options);
        }

        public static IServiceCollection AddBlobResourceTokenSupport(this IServiceCollection services, Action<IdentityServiceOptions> options)
        {
            services.AddIdentity(options);
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

        public static IServiceCollection AddBlobResourceTokenSupport(this IServiceCollection services, IConfiguration identityConfiguration)
            => services.AddBlobResourceTokenSupport(identityConfiguration.Bind);

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