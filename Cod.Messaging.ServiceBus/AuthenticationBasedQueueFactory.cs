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
        private static readonly ConcurrentDictionary<string, ServiceBusSender> senders = [];
        private readonly DefaultAzureCredential defaultCredential = new(includeInteractiveCredentials: options.Value.EnableInteractiveIdentity);
        private bool disposed;

        public async Task<ServiceBusSender> CreateQueueAsync(string name, CancellationToken cancellationToken = default)
        {
            if (!string.IsNullOrWhiteSpace(options.Value.FullyQualifiedNamespace))
            {
                return senders.GetOrAdd(name, n =>
                            clients.GetOrAdd(name, new ServiceBusClient(
                                    options.Value.FullyQualifiedNamespace,
                                    defaultCredential,
                                    options: CreateOptions(options.Value)))
                                .CreateSender(name));
            }
            else
            {
                var permissions = await authenticator.Value.GetResourcePermissionsAsync(cancellationToken)
                    ?? throw new ApplicationException(InternalError.AuthenticationRequired);
                var permission = permissions.FirstOrDefault(p => p.Type == ResourceType.AzureServiceBus
                    && (p.IsWildcard || p.Scope == name)
                    && p.Entitlements.Contains(Constants.EntitlementMessagingSend))
                    ?? throw new ApplicationException(InternalError.Forbidden);
                var token = await authenticator.Value.RetrieveResourceTokenAsync(ResourceType.AzureServiceBus, permission.Resource, partition: name, cancellationToken: cancellationToken)
                    ?? throw new ApplicationException(InternalError.Forbidden);
                return senders.GetOrAdd(name, n =>
                            clients.GetOrAdd(name, new ServiceBusClient(
                                    permission.Resource,
                                    new AzureSasCredential($"SharedAccessSignature {token}"),
                                    options: CreateOptions(options.Value)))
                                .CreateSender(name));
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