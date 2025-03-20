using Azure.Identity;
using Azure.Storage.Blobs;
using Cod.Identity;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace Cod.File.Blob
{
    internal class AzureBlobClientFactory(IOptions<StorageBlobOptions> options, Lazy<IAuthenticator> authenticator)
    {
        private static readonly ConcurrentDictionary<string, BlobServiceClient> clients = [];

        public async Task<BlobServiceClient> CreateClientAsync(IEnumerable<FilePermissions> permissions, string containerName, CancellationToken cancellationToken = default)
        {
            if (!String.IsNullOrWhiteSpace(options.Value.FullyQualifiedDomainName))
            {
                return await CreateClientAsync(cancellationToken);
            }

            var resourcePermissions = await authenticator.Value.GetResourcePermissionsAsync(cancellationToken) ?? [];
            var permission = resourcePermissions.FirstOrDefault(p => 
                p.Type == ResourceType.AzureStorageBlob 
                && permissions.All(m => p.Entitlements.Contains(m.ToString().ToUpperInvariant()))
                && containerName.StartsWith(p.Partition))
                ?? throw new ApplicationException(InternalError.Forbidden);
            var sas = await authenticator.Value.RetrieveResourceTokenAsync(ResourceType.AzureStorageBlob, permission.Resource, partition: containerName, cancellationToken: cancellationToken);
            var builder = new BlobUriBuilder(new Uri($"https://{permission.Resource}"))
            {
                 Query = sas,
            };
            var serviceUri = builder.ToUri();
            return clients.GetOrAdd(containerName, new BlobServiceClient(serviceUri, options: BuildClientOptions(options)));
        }

        public Task<BlobServiceClient> CreateClientAsync(CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(options.Value.FullyQualifiedDomainName))
            {
                throw new ApplicationException(InternalError.InternalServerError);
            }

            var client = clients.GetOrAdd(options.Value.FullyQualifiedDomainName, _ =>
            {
                var opt = BuildClientOptions(options);
                return Uri.TryCreate($"https://{options.Value.FullyQualifiedDomainName}", UriKind.Absolute, out var endpointUri)
                    ? new BlobServiceClient(endpointUri, new DefaultAzureCredential(includeInteractiveCredentials: options.Value.EnableInteractiveIdentity), opt)
                    : throw new ApplicationException(InternalError.InternalServerError);
            });
            
            return Task.FromResult(client);
        }

        private static BlobClientOptions BuildClientOptions(IOptions<StorageBlobOptions> options)
        {
            var opt = new BlobClientOptions();
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