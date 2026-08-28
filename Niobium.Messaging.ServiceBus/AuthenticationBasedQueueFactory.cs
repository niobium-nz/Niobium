using System.Collections.Concurrent;
using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Niobium.Identity;

namespace Niobium.Messaging.ServiceBus
{
    internal sealed class AuthenticationBasedQueueFactory(
        Lazy<IAuthenticator> authenticator,
        IConfiguration config,
        IOptions<ServiceBusOptions> options)
    {
        private const string DefaultServiceBusFQDNSetting = "AzureWebJobsServiceBus:fullyQualifiedNamespace";
        private const string ManagedIdentitySetting = "AzureWebJobsStorage:clientId";
        private static readonly ConcurrentDictionary<string, ServiceBusClient> clients = [];
        private static readonly ConcurrentDictionary<string, TokenCredential> credentials = [];
        private static readonly Dictionary<string, ServiceBusSender> senders = [];
        private static readonly Dictionary<string, ServiceBusReceiver> receivers = [];

        public ServiceBusOptions Configuration
        {
            get => field ?? options.Value;
            set
            {
                if (value != null)
                {
                    field = value;
                }
            }
        }

        public async Task<ServiceBusReceiver> CreateReceiverAsync(IEnumerable<MessagingPermissions> permissions, string name, CancellationToken cancellationToken = default)
        {
            if (receivers.TryGetValue(name, out ServiceBusReceiver? cache))
            {
                return cache;
            }

            ServiceBusClient client = await this.CreateClientAsync(permissions, name, cancellationToken);
            ServiceBusReceiver receiver = client.CreateReceiver(name);
            receivers.Add(name, receiver);
            return receiver;
        }

        public async Task<ServiceBusSender> CreateSenderAsync(IEnumerable<MessagingPermissions> permissions, string name, CancellationToken cancellationToken = default)
        {
            if (senders.TryGetValue(name, out ServiceBusSender? cache))
            {
                return cache;
            }

            ServiceBusClient client = await this.CreateClientAsync(permissions, name, cancellationToken);
            ServiceBusSender sender = client.CreateSender(name);
            senders.Add(name, sender);
            return sender;
        }

        private async Task<ServiceBusClient> CreateClientAsync(IEnumerable<MessagingPermissions> permissions, string name, CancellationToken cancellationToken = default)
        {
            string? fqdn = this.Configuration.FullyQualifiedNamespace ?? config[DefaultServiceBusFQDNSetting];
            if (!String.IsNullOrWhiteSpace(fqdn))
            {
                DefaultAzureCredentialOptions credentialOptions = new()
                {
                    ExcludeInteractiveBrowserCredential = !this.Configuration.EnableInteractiveIdentity,
                };

                string? clientId = config[ManagedIdentitySetting];
                if (!String.IsNullOrWhiteSpace(clientId))
                {
                    credentialOptions.ManagedIdentityClientId = clientId;
                }

                TokenCredential credential = credentials.GetOrAdd(fqdn, new DefaultAzureCredential(credentialOptions));
                return clients.GetOrAdd(fqdn, new ServiceBusClient(fqdn, credential, options: CreateOptions(this.Configuration)));
            }
            else
            {
                ResourcePermission[] resourcePermissions = [.. await authenticator.Value.GetResourcePermissionsAsync(cancellationToken)];
                ResourcePermission permission = resourcePermissions.FirstOrDefault(p =>
                        p.Type == ResourceType.AzureServiceBus
                        && permissions.All(m => p.Entitlements.Contains(m.ToString().ToUpperInvariant()))
                        && p.Partition != null
                        && name.StartsWith(p.Partition))
                    ?? throw new ApplicationException(InternalError.Forbidden);
                string token = await authenticator.Value.RetrieveResourceTokenAsync(ResourceType.AzureServiceBus, permission.Resource, partition: name, cancellationToken: cancellationToken);
                return clients.GetOrAdd(name, new ServiceBusClient(
                    permission.Resource,
                    new AzureSasCredential($"SharedAccessSignature {token}"),
                    options: CreateOptions(this.Configuration)));
            }
        }

        private static ServiceBusClientOptions CreateOptions(ServiceBusOptions options)
        {
            ServiceBusClientOptions result = new()
            {
                TransportType = options.UseWebSocket ? ServiceBusTransportType.AmqpWebSockets : ServiceBusTransportType.AmqpTcp,
                RetryOptions = new ServiceBusRetryOptions
                {
                    Mode = ServiceBusRetryMode.Exponential,
                    MaxDelay = options.MaxDelay,
                    MaxRetries = options.MaxRetries,
                }
            };

            if (options.ConnectionIdleTimeout.HasValue)
            {
                result.ConnectionIdleTimeout = options.ConnectionIdleTimeout.Value;
            }

            return result;
        }
    }
}