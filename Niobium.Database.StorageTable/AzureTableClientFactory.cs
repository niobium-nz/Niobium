using Azure.Core;
using Azure.Data.Tables;
using Azure.Identity;
using Microsoft.Extensions.Options;
using Niobium.Identity;
using System.Collections.Concurrent;

namespace Niobium.Database.StorageTable
{
    internal sealed class AzureTableClientFactory(IOptions<StorageTableOptions> options, Lazy<IAuthenticator> authenticator) : IAzureTableClientFactory
    {
        private static readonly ConcurrentDictionary<string, TableServiceClient> clients = [];
        private static readonly ConcurrentDictionary<string, TokenCredential> credentials = [];

        public async Task<TableServiceClient> CreateClientAsync(IEnumerable<DatabasePermissions> permissions, string table, string? partition = null, CancellationToken cancellationToken = default)
        {
            if (!string.IsNullOrWhiteSpace(options.Value.FullyQualifiedDomainName))
            {
                return await CreateClientAsync(cancellationToken);
            }

            IEnumerable<ResourcePermission> resourcePermissions = await authenticator.Value.GetResourcePermissionsAsync(cancellationToken) ?? [];
            ResourcePermission permission = resourcePermissions.FirstOrDefault(p =>
                p.Type == ResourceType.AzureStorageTable
                && p.Partition == table
                && permissions.All(m => p.Entitlements.Contains(m.ToString().ToUpperInvariant()))
                && (partition == null || p.Scope == null || partition.StartsWith(p.Scope)))
                ?? throw new ApplicationException(InternalError.Forbidden);
            string sasUri = await authenticator.Value.RetrieveResourceTokenAsync(ResourceType.AzureStorageTable, table, partition: partition ?? permission.Scope, cancellationToken: cancellationToken);
            Uri endpoint = new($"https://{permission.Resource}/{table}?{sasUri}");
            return clients.GetOrAdd($"{table}//{partition ?? string.Empty}", new TableServiceClient(endpoint, options: BuildClientOptions(options)));
        }

        private Task<TableServiceClient> CreateClientAsync(CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(options.Value.FullyQualifiedDomainName))
            {
                throw new ApplicationException(InternalError.InternalServerError);
            }

            TableServiceClient client = clients.GetOrAdd(options.Value.FullyQualifiedDomainName, _ =>
            {
                TableClientOptions opt = BuildClientOptions(options);
                TokenCredential credential = credentials.GetOrAdd(options.Value.FullyQualifiedDomainName, 
                    _ => new DefaultAzureCredential(includeInteractiveCredentials: options.Value.EnableInteractiveIdentity));
                return Uri.TryCreate($"https://{options.Value.FullyQualifiedDomainName}", UriKind.Absolute, out Uri? endpointUri)
                    ? new TableServiceClient(endpointUri, credential, opt)
                    : throw new ApplicationException(InternalError.InternalServerError);
            });

            return Task.FromResult(client);
        }

        private static TableClientOptions BuildClientOptions(IOptions<StorageTableOptions> options)
        {
            TableClientOptions opt = new();
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