using Azure.Core;
using Azure.Data.Tables;
using Azure.Identity;
using Cod.Identity;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace Cod.Database.StorageTable
{
    internal class AzureTableClientFactory(IOptions<StorageTableOptions> options, Lazy<IAuthenticator> authenticator) : IAzureTableClientFactory
    {
        private static readonly ConcurrentDictionary<string, TableServiceClient> clients = [];
        private static readonly ConcurrentDictionary<string, TokenCredential> credentials = [];

        public async Task<TableServiceClient> CreateClientAsync(IEnumerable<DatabasePermissions> permissions, string table, string partition = null, CancellationToken cancellationToken = default)
        {
            if (!String.IsNullOrWhiteSpace(options.Value.FullyQualifiedDomainName))
            {
                return await CreateClientAsync(cancellationToken);
            }

            var resourcePermissions = await authenticator.Value.GetResourcePermissionsAsync(cancellationToken) ?? [];
            var permission = resourcePermissions.FirstOrDefault(p =>
                p.Type == ResourceType.AzureStorageTable
                && p.Partition == table
                && permissions.All(m => p.Entitlements.Contains(m.ToString().ToUpperInvariant()))
                && (partition == null || p.Scope == null || partition.StartsWith(p.Scope)))
                ?? throw new ApplicationException(InternalError.Forbidden);
            var sasUri = await authenticator.Value.RetrieveResourceTokenAsync(ResourceType.AzureStorageTable, table, partition: partition ?? permission.Scope, cancellationToken: cancellationToken);
            var endpoint = new Uri($"https://{permission.Resource}/{table}?{sasUri}");
            return clients.GetOrAdd($"{table}//{partition ?? string.Empty}", new TableServiceClient(endpoint, options: BuildClientOptions(options)));
        }

        private Task<TableServiceClient> CreateClientAsync(CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(options.Value.FullyQualifiedDomainName))
            {
                throw new ApplicationException(InternalError.InternalServerError);
            }

            var client = clients.GetOrAdd(options.Value.FullyQualifiedDomainName, _ =>
            {
                var opt = BuildClientOptions(options);
                var credential = credentials.GetOrAdd(options.Value.FullyQualifiedDomainName, _ => new DefaultAzureCredential(includeInteractiveCredentials: options.Value.EnableInteractiveIdentity));
                return Uri.TryCreate($"https://{options.Value.FullyQualifiedDomainName}", UriKind.Absolute, out var endpointUri)
                    ? new TableServiceClient(endpointUri, new DefaultAzureCredential(includeInteractiveCredentials: options.Value.EnableInteractiveIdentity), opt)
                    : throw new ApplicationException(InternalError.InternalServerError);
            });

            return Task.FromResult(client);
        }

        private static TableClientOptions BuildClientOptions(IOptions<StorageTableOptions> options)
        {
            var opt = new TableClientOptions();
            opt.Retry.MaxDelay = options.Value.MaxDelay;
            opt.Retry.MaxRetries = options.Value.MaxRetries;
            if (options.Value.ConnectionIdleTimeout.HasValue)
            {
                opt.Retry.NetworkTimeout = options.Value.ConnectionIdleTimeout.Value;
            }

            return opt;
        }
    }
}