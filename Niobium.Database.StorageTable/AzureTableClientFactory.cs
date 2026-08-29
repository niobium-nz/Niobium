using System.Collections.Concurrent;
using Azure.Core;
using Azure.Data.Tables;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Niobium.Identity;

namespace Niobium.Database.StorageTable
{
    internal sealed class AzureTableClientFactory(IOptions<StorageTableOptions> options, IConfiguration configuration, Lazy<IAuthenticator> authenticator) : IAzureTableClientFactory
    {
        private static readonly ConcurrentDictionary<string, TableServiceClient> clients = [];
        private static readonly ConcurrentDictionary<string, TokenCredential> credentials = [];

        public async Task<TableServiceClient> CreateClientAsync(IEnumerable<DatabasePermissions> permissions, string table, string? partition = null, CancellationToken cancellationToken = default)
        {
            if (!String.IsNullOrWhiteSpace(options.Value.FullyQualifiedDomainName)
                || !String.IsNullOrWhiteSpace(configuration[Constants.DefaultTableServiceUriSetting]))
            {
                return await this.CreateClientAsync(cancellationToken);
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
            return clients.GetOrAdd($"{table}//{partition ?? String.Empty}", new TableServiceClient(endpoint, options: BuildClientOptions(options)));
        }

        private Task<TableServiceClient> CreateClientAsync(CancellationToken cancellationToken = default)
        {
            options.Value.FullyQualifiedDomainName ??= configuration[Constants.DefaultTableServiceUriSetting]
                ?? throw new ApplicationException(InternalError.InternalServerError, "Fully qualified domain name is not specified");
            if (!Uri.TryCreate(options.Value.FullyQualifiedDomainName, UriKind.Absolute, out Uri? endpointUri)
                && !Uri.TryCreate($"https://{options.Value.FullyQualifiedDomainName}", UriKind.Absolute, out endpointUri))
            {
                throw new ApplicationException(InternalError.InternalServerError, "Invalid table service URI");
            }

            TableServiceClient client = clients.GetOrAdd(endpointUri.AbsoluteUri, _ =>
            {
                TableClientOptions opt = BuildClientOptions(options);
                TokenCredential credential = credentials.GetOrAdd(endpointUri.AbsoluteUri,
                    _ =>
                    {
                        DefaultAzureCredentialOptions credentialOptions = new()
                        {
                            ExcludeInteractiveBrowserCredential = !options.Value.EnableInteractiveIdentity,
                        };

                        string? clientId = configuration[Constants.ManagedIdentitySetting];
                        if (!String.IsNullOrWhiteSpace(clientId))
                        {
                            credentialOptions.ManagedIdentityClientId = clientId;
                        }

                        return new DefaultAzureCredential(credentialOptions);
                    });
                return new TableServiceClient(endpointUri, credential, opt);
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