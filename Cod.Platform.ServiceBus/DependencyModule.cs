using Cod.Identity;
using Cod.Platform;
using Cod.Platform.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cod.Messaging.ServiceBus
{
    public static class DependencyModule
    {
        public static IServiceCollection AddMessaging(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddCodPlatform();
            return services.AddMessaging(configuration.Bind);
        }

        public static IServiceCollection AddServiceBusResourceTokenSupport(this IServiceCollection services, Action<IdentityServiceOptions> options)
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
        {
            return services.AddTransient<IEntitlementDescriptor>(sp =>
            {
                var role = resolveRole(sp);
                var queue = resolveQueueName(sp);
                var fullyQualifiedNamespace = resolvefullyQualifiedNamespace(sp);
                return new RoleBasedSendEntitlementDescriptor(role, fullyQualifiedNamespace, queue, permissions);
            });
        }

        public static IServiceCollection AddServiceBusResourceTokenSupport(this IServiceCollection services, IConfiguration identityConfiguration)
            => services.AddServiceBusResourceTokenSupport(identityConfiguration.Bind);

        public static IServiceCollection GrantServiceBusSendEntitlementTo(this IServiceCollection services, string role, string queueName, string fullyQualifiedNamespace, MessagingPermissions permissions)
            => services.GrantServiceBusEntitlementTo(_ => role, _ => queueName, _ => fullyQualifiedNamespace, permissions);
    }
}