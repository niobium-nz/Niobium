using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Niobium.Identity;
using Niobium.Messaging;
using Niobium.Messaging.ServiceBus;
using Niobium.Platform.Identity;

namespace Niobium.Platform.ServiceBus
{
    public static class DependencyModule
    {
        private static volatile bool loaded;

        public static void AddMessaging(this IHostApplicationBuilder builder, Action<ServiceBusOptions>? options = null)
        {
            options ??= builder.Configuration.GetSection(nameof(ServiceBusOptions)).Bind;
            bool isDevEnv = builder.Configuration.IsDevelopmentEnvironment();
            builder.Services.AddMessaging(testMode: isDevEnv, options: options);

            if (isDevEnv)
            {
                builder.Services.PostConfigure<ServiceBusOptions>(options =>
                {
                    options.EnableInteractiveIdentity = true;
                });
            }
        }

        public static IServiceCollection AddMessaging(this IServiceCollection services, bool testMode = false, Action<ServiceBusOptions>? options = null)
        {
            if (loaded)
            {
                return services;
            }

            loaded = true;

            services.AddPlatform();
            return Messaging.ServiceBus.DependencyModule.AddMessaging(services, testMode: testMode, options: options);
        }

        public static IServiceCollection AddServiceBusResourceTokenSupport(this IHostApplicationBuilder builder)
            => builder.Services.AddServiceBusResourceTokenSupport(builder.Configuration.GetSection(nameof(IdentityServiceOptions)).Bind);

        public static IServiceCollection AddServiceBusResourceTokenSupport(this IServiceCollection services, Action<IdentityServiceOptions>? options = null)
        {
            services.AddIdentity(options);
            services.AddTransient<ISignatureIssuer, ServiceBusSignatureIssuer>();
            services.AddTransient<IResourceControl, DefaultServiceBusQueueControl>();
            return services;
        }

        public static IServiceCollection GrantServiceBusEntitlementTo(
            this IServiceCollection services,
            Func<IServiceProvider, string> resolveRole,
            Func<IServiceProvider, string> resolveQueueName,
            Func<IServiceProvider, string> resolvefullyQualifiedNamespace,
            MessagingPermissions permissions)
            => services.AddTransient<IEntitlementDescriptor>(sp =>
                {
                    string role = resolveRole(sp);
                    string queue = resolveQueueName(sp);
                    string fullyQualifiedNamespace = ValidateFullyQualifiedDomainName(sp, resolvefullyQualifiedNamespace);
                    return new RoleBasedSendEntitlementDescriptor(role, fullyQualifiedNamespace, queue, permissions);
                });

        public static IServiceCollection AddServiceBusResourceTokenSupport(this IServiceCollection services, IConfiguration identityConfiguration)
            => services.AddServiceBusResourceTokenSupport(identityConfiguration.Bind);

        public static IServiceCollection GrantServiceBusSendEntitlementTo(this IServiceCollection services, string role, string queueName, string fullyQualifiedNamespace, MessagingPermissions permissions)
            => services.GrantServiceBusEntitlementTo(_ => role, _ => queueName, _ => fullyQualifiedNamespace, permissions);

        private static string ValidateFullyQualifiedDomainName(IServiceProvider sp, Func<IServiceProvider, string?> resolvefullyQualifiedDomainName)
        {
            string? fdqn = resolvefullyQualifiedDomainName(sp);

            if (fdqn != null)
            {
                IConfiguration config = sp.GetRequiredService<IConfiguration>();
                string? uri = config[Messaging.ServiceBus.Constants.DefaultServiceBusFQDNSetting];
                if (!Uri.TryCreate(uri, UriKind.Absolute, out Uri? parsedUri))
                {
                    throw new InvalidOperationException($"Invalid URI for service bus: {uri}");
                }
                fdqn = parsedUri.Host;
            }
            return fdqn ?? throw new InvalidOperationException("Fully qualified domain name is not available");
        }
    }
}