using System.Collections.Concurrent;
using Azure.Core;
using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;
using Niobium.Identity;

namespace Niobium.File.Blob
{
    internal sealed class AzureBlobClientFactory(IOptions<StorageBlobOptions> options, Lazy<IAuthenticator> authenticator)
    {
        private const string DefaultBlobServiceUriSetting = "AzureWebJobsStorage__blobServiceUri";
        private const string ManagedIdentitySetting = "AzureWebJobsStorage__clientId";
        private static readonly ConcurrentDictionary<string, BlobServiceClient> clients = [];
        private static readonly ConcurrentDictionary<string, TokenCredential> credentials = [];

        public async Task<BlobServiceClient> CreateClientAsync(IEnumerable<FilePermissions> permissions, string containerName, CancellationToken cancellationToken = default)
        {
            if (!String.IsNullOrWhiteSpace(options.Value.FullyQualifiedDomainName)
                || !String.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(DefaultBlobServiceUriSetting)))
            {
                return await this.CreateClientAsync(cancellationToken);
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
            options.Value.FullyQualifiedDomainName ??= Environment.GetEnvironmentVariable(DefaultBlobServiceUriSetting)
                ?? throw new ApplicationException(InternalError.InternalServerError, "Fully qualified domain name is not specified");
            if (!Uri.TryCreate(options.Value.FullyQualifiedDomainName, UriKind.Absolute, out Uri? endpointUri)
                && !Uri.TryCreate($"https://{options.Value.FullyQualifiedDomainName}", UriKind.Absolute, out endpointUri))
            {
                throw new ApplicationException(InternalError.InternalServerError, "Invalid blob service URI");
            }

            BlobServiceClient client = clients.GetOrAdd(options.Value.FullyQualifiedDomainName, _ =>
            {
                BlobClientOptions opt = BuildClientOptions(options);
                TokenCredential credential = credentials.GetOrAdd(options.Value.FullyQualifiedDomainName,
                    _ =>
                    {
                        DefaultAzureCredentialOptions credentialOptions = new()
                        {
                            ExcludeInteractiveBrowserCredential = !options.Value.EnableInteractiveIdentity,
                        };

                        string? clientId = Environment.GetEnvironmentVariable(ManagedIdentitySetting);
                        if (!String.IsNullOrWhiteSpace(clientId))
                        {
                            credentialOptions.ManagedIdentityClientId = clientId;
                        }

                        return new DefaultAzureCredential(credentialOptions);
                    });
                return new BlobServiceClient(endpointUri, credential, opt);
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