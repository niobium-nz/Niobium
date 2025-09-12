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
            var testMode = builder.Configuration.IsPreProductionEnvironment();
            builder.Services.AddMessaging(testMode: testMode, options: options);

            if (builder.Configuration.IsPreProductionEnvironment())
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
        {
            return builder.Services.AddServiceBusResourceTokenSupport(builder.Configuration.GetSection(nameof(IdentityServiceOptions)).Bind);
        }

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
        {
            return services.AddTransient<IEntitlementDescriptor>(sp =>
            {
                string role = resolveRole(sp);
                string queue = resolveQueueName(sp);
                string fullyQualifiedNamespace = resolvefullyQualifiedNamespace(sp);
                return new RoleBasedSendEntitlementDescriptor(role, fullyQualifiedNamespace, queue, permissions);
            });
        }

        public static IServiceCollection AddServiceBusResourceTokenSupport(this IServiceCollection services, IConfiguration identityConfiguration)
        {
            return services.AddServiceBusResourceTokenSupport(identityConfiguration.Bind);
        }

        public static IServiceCollection GrantServiceBusSendEntitlementTo(this IServiceCollection services, string role, string queueName, string fullyQualifiedNamespace, MessagingPermissions permissions)
        {
            return services.GrantServiceBusEntitlementTo(_ => role, _ => queueName, _ => fullyQualifiedNamespace, permissions);
        }
    }
}