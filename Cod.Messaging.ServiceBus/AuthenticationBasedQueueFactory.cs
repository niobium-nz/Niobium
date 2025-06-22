using Azure;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Cod.Identity;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace Cod.Messaging.ServiceBus
{
    internal class AuthenticationBasedQueueFactory(
        Lazy<IAuthenticator> authenticator,
        IOptions<ServiceBusOptions> options)
        : IAsyncDisposable
    {
        private static readonly ConcurrentDictionary<string, ServiceBusClient> clients = [];
        private static readonly Dictionary<string, ServiceBusSender> senders = [];
        private static readonly Dictionary<string, ServiceBusReceiver> receivers = [];
        private DefaultAzureCredential? defaultCredential;
        private ServiceBusOptions? configuration;
        private bool disposed;

        public ServiceBusOptions Configuration
        {
            get
            {
                if (configuration != null)
                {
                    return configuration;
                }
                else
                {
                    return options.Value;
                }
            }
            set
            {
                if (value != null)
                {
                    configuration = value;
                }
            }
        }

        protected DefaultAzureCredential Credential
        {
            get
            {
                defaultCredential ??= new(includeInteractiveCredentials: Configuration.EnableInteractiveIdentity);
                return defaultCredential;
            }
            set
            {
                if (value != null)
                {
                    defaultCredential = value;
                }
            }
        }

        public async Task<ServiceBusReceiver> CreateReceiverAsync(IEnumerable<MessagingPermissions> permissions, string name, CancellationToken cancellationToken = default)
        {
            if (receivers.TryGetValue(name, out var cache))
            {
                return cache;
            }

            var client = await CreateClientAsync(permissions, name, cancellationToken);
            var receiver = client.CreateReceiver(name);
            receivers.Add(name, receiver);
            return receiver;
        }

        public async Task<ServiceBusSender> CreateSenderAsync(IEnumerable<MessagingPermissions> permissions, string name, CancellationToken cancellationToken = default)
        {
            if (senders.TryGetValue(name, out var cache))
            {
                return cache;
            }

            var client = await CreateClientAsync(permissions, name, cancellationToken);
            var sender = client.CreateSender(name);
            senders.Add(name, sender);
            return sender;
        }

        private async Task<ServiceBusClient> CreateClientAsync(IEnumerable<MessagingPermissions> permissions, string name, CancellationToken cancellationToken = default)
        {
            if (!string.IsNullOrWhiteSpace(Configuration.FullyQualifiedNamespace))
            {
                return clients.GetOrAdd(name, new ServiceBusClient(
                    Configuration.FullyQualifiedNamespace,
                    Credential,
                    options: CreateOptions(Configuration)));
            }
            else
            {
                var resourcePermissions = (await authenticator.Value.GetResourcePermissionsAsync(cancellationToken)).ToArray();
                var permission = resourcePermissions.FirstOrDefault(p =>
                        p.Type == ResourceType.AzureServiceBus
                        && permissions.All(m => p.Entitlements.Contains(m.ToString().ToUpperInvariant()))
                        && name.StartsWith(p.Partition))
                    ?? throw new ApplicationException(InternalError.Forbidden);
                var token = await authenticator.Value.RetrieveResourceTokenAsync(ResourceType.AzureServiceBus, permission.Resource, partition: name, cancellationToken: cancellationToken);
                return clients.GetOrAdd(name, new ServiceBusClient(
                    permission.Resource,
                    new AzureSasCredential($"SharedAccessSignature {token}"),
                    options: CreateOptions(Configuration)));
            }
        }

        private static ServiceBusClientOptions CreateOptions(ServiceBusOptions options)
        {
            var result = new ServiceBusClientOptions
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

        protected virtual async ValueTask DisposeAsync(bool disposing)
        {
            if (disposing)
            {
                foreach (var receiver in receivers.Keys)
                {
                    await receivers[receiver].DisposeAsync();
                }
                receivers.Clear();

                foreach (var sender in senders.Keys)
                {
                    await senders[sender].DisposeAsync();
                }
                senders.Clear();

                foreach (var client in clients.Keys)
                {
                    await clients[client].DisposeAsync();
                }
                clients.Clear();
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (!disposed)
            {
                await DisposeAsync(true);
            }

            disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}