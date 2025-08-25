using Azure.Core;
using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;
using Niobium.Identity;
using System.Collections.Concurrent;

namespace Niobium.File.Blob
{
    internal sealed class AzureBlobClientFactory(IOptions<StorageBlobOptions> options, Lazy<IAuthenticator> authenticator)
    {
        private static readonly ConcurrentDictionary<string, BlobServiceClient> clients = [];
        private static readonly ConcurrentDictionary<string, TokenCredential> credentials = [];

        public async Task<BlobServiceClient> CreateClientAsync(IEnumerable<FilePermissions> permissions, string containerName, CancellationToken cancellationToken = default)
        {
            if (!string.IsNullOrWhiteSpace(options.Value.FullyQualifiedDomainName))
            {
                return await CreateClientAsync(cancellationToken);
            }

            IEnumerable<ResourcePermission> resourcePermissions = await authenticator.Value.GetResourcePermissionsAsync(cancellationToken) ?? [];
            ResourcePermission permission = resourcePermissions.FirstOrDefault(p =>
                p.Type == ResourceType.AzureStorageBlob
                && permissions.All(m => p.Entitlements.Contains(m.ToString().ToUpperInvariant()))
                && p.Partition != null
                && containerName.StartsWith(p.Partition))
                ?? throw new ApplicationException(InternalError.Forbidden);
            string sas = await authenticator.Value.RetrieveResourceTokenAsync(ResourceType.AzureStorageBlob, permission.Resource, partition: containerName, cancellationToken: cancellationToken);
            BlobUriBuilder builder = new(new Uri($"https://{permission.Resource}"))
            {
                Query = sas,
            };
            Uri serviceUri = builder.ToUri();
            return clients.GetOrAdd(containerName, new BlobServiceClient(serviceUri, options: BuildClientOptions(options)));
        }

        public Task<BlobServiceClient> CreateClientAsync(CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(options.Value.FullyQualifiedDomainName))
            {
                throw new ApplicationException(InternalError.InternalServerError);
            }

            BlobServiceClient client = clients.GetOrAdd(options.Value.FullyQualifiedDomainName, _ =>
            {
                BlobClientOptions opt = BuildClientOptions(options);
                TokenCredential credential = credentials.GetOrAdd(options.Value.FullyQualifiedDomainName, _ => new DefaultAzureCredential(includeInteractiveCredentials: options.Value.EnableInteractiveIdentity));
                return Uri.TryCreate($"https://{options.Value.FullyQualifiedDomainName}", UriKind.Absolute, out Uri? endpointUri)
                    ? new BlobServiceClient(endpointUri, credential, opt)
                    : throw new ApplicationException(InternalError.InternalServerError);
            });

            return Task.FromResult(client);
        }

        private static BlobClientOptions BuildClientOptions(IOptions<StorageBlobOptions> options)
        {
            BlobClientOptions opt = new();
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