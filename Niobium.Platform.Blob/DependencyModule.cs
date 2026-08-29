using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Niobium.File;
using Niobium.File.Blob;
using Niobium.Identity;
using Niobium.Platform.Identity;

namespace Niobium.Platform.Blob
{
    public static class DependencyModule
    {
        private static volatile bool loaded;

        public static void AddFile(this IHostApplicationBuilder builder)
        {
            builder.Services.AddFile(builder.Configuration.GetSection(nameof(StorageBlobOptions)).Bind);

            bool isDevelopment = builder.Configuration.IsDevelopmentEnvironment();
            if (isDevelopment)
            {
                builder.Services.PostConfigure<StorageBlobOptions>(opt => opt.EnableInteractiveIdentity = true);
            }
        }

        public static IServiceCollection AddFile(this IServiceCollection services, Action<StorageBlobOptions>? options = null)
        {
            if (loaded)
            {
                return services;
            }

            loaded = true;
            services.AddPlatform();

            return File.Blob.DependencyModule.AddFile(services, options);
        }

        public static IServiceCollection AddBlobResourceTokenSupport(this IHostApplicationBuilder builder)
            => builder.Services.AddBlobResourceTokenSupport(builder.Configuration.GetSection(nameof(IdentityServiceOptions)).Bind);

        public static IServiceCollection AddBlobResourceTokenSupport(this IServiceCollection services, Action<IdentityServiceOptions>? options = null)
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
            Func<IServiceProvider, string?> resolvefullyQualifiedDomainName)
            => services.AddTransient<IEntitlementDescriptor>(sp =>
                {
                    string role = resolveRole(sp);
                    string container = resolveContainerName(sp);
                    string fullyQualifiedDomainName = ValidateFullyQualifiedDomainName(sp, resolvefullyQualifiedDomainName);
                    return new RoleBasedEntitlementDescriptor(role, permissions, fullyQualifiedDomainName, container);
                });

        public static IServiceCollection GrantBlobPersonalizedEntitlementTo(
            this IServiceCollection services,
            Func<IServiceProvider, string> resolveRole,
            FilePermissions permissions,
            Func<IServiceProvider, IEnumerable<string>> resolveContainerNamePrefix,
            Func<IServiceProvider, string?> resolvefullyQualifiedDomainName)
            => services.AddTransient<IEntitlementDescriptor>(sp =>
                {
                    string role = resolveRole(sp);
                    IEnumerable<string> containerNamePrefix = resolveContainerNamePrefix(sp);
                    string fullyQualifiedDomainName = ValidateFullyQualifiedDomainName(sp, resolvefullyQualifiedDomainName);
                    return new PersonalizedEntitlementDescriptor(role, permissions, fullyQualifiedDomainName, containerNamePrefix);
                });

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
            Func<IServiceProvider, string?> resolvefullyQualifiedDomainName)
            => services.GrantBlobPersonalizedEntitlementTo(resolveRole, permissions, sp => [resolveContainerNamePrefix(sp)], resolvefullyQualifiedDomainName);

        private static string ValidateFullyQualifiedDomainName(IServiceProvider sp, Func<IServiceProvider, string?> resolvefullyQualifiedDomainName)
        {
            string? fdqn = resolvefullyQualifiedDomainName(sp);

            if (fdqn != null)
            {
                IConfiguration config = sp.GetRequiredService<IConfiguration>();
                string? uri = config[File.Blob.Constants.DefaultBlobServiceUriSetting];
                if (!Uri.TryCreate(uri, UriKind.Absolute, out Uri? parsedUri))
                {
                    throw new InvalidOperationException($"Invalid URI for blob service: {uri}");
                }
                fdqn = parsedUri.Host;
            }
            return fdqn ?? throw new InvalidOperationException("Fully qualified domain name is not available");
        }
    }
}