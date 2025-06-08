using Cod.Identity;
using Cod.Messaging;
using Cod.Messaging.ServiceBus;
using Cod.Platform.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cod.Platform.ServiceBus
{
    public static class DependencyModule
    {
        private static volatile bool loaded;

        public static void AddMessaging(this IHostApplicationBuilder builder)
        {
            builder.Services.AddMessaging(builder.Configuration.GetSection(nameof(ServiceBusOptions)).Bind);

            if (builder.Configuration.IsDevelopmentEnvironment())
            {
                builder.Services.PostConfigure<ServiceBusOptions>(options =>
                {
                    options.EnableInteractiveIdentity = true;
                });
            }
        }

        public static IServiceCollection AddMessaging(this IServiceCollection services, Action<ServiceBusOptions>? options)
        {
            if (loaded)
            {
                return services;
            }

            loaded = true;

            services.AddPlatform();
            return Cod.Messaging.ServiceBus.DependencyModule.AddMessaging(services, options);
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