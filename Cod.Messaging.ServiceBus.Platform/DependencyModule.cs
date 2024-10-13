using Cod.Identity;
using Cod.Platform;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cod.Messaging.ServiceBus
{
    public static class DependencyModule
    {
        public static IServiceCollection AddServiceBusResourceTokenSupport(this IServiceCollection services)
        {
            services.AddCodPlatform();
            services.AddTransient<ISignatureIssuer, ServiceBusSignatureIssuer>();
            services.AddTransient<IResourceControl, ServiceBusQueueControl>();
            return services;
        }

        public static IServiceCollection GrantServiceBusSendEntitlementTo(
            this IServiceCollection services,
            Func<IServiceProvider, string> resolveRole,
            Func<IServiceProvider, string> resolveQueueName,
            Func<IServiceProvider, string> resolvefullyQualifiedNamespace)
        {
            return services.AddTransient<IEntitlementDescriptor>(sp =>
            {
                var role = resolveRole(sp);
                var queue = resolveQueueName(sp);
                var fullyQualifiedNamespace = resolvefullyQualifiedNamespace(sp);
                return new RoleBasedSendEntitlementDescriptor(role, fullyQualifiedNamespace, queue);
            });
        }

        public static IServiceCollection GrantServiceBusSendEntitlementTo(this IServiceCollection services, string role, string queueName, string fullyQualifiedNamespace)
            => services.GrantServiceBusSendEntitlementTo(_ => role, _ => queueName, _ => fullyQualifiedNamespace);

        public static IServiceCollection AddMessaging(this IServiceCollection services, IConfiguration configuration)
            => services.AddMessaging(configuration.Bind);
    }
}