using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Runtime.CompilerServices;

namespace Cod.Platform.Integration.Azure
{
    public class CloudBlobRepository : IBlobRepository
    {
        protected BlobServiceClient Client { get; private set; }
        protected bool CreateIfNotExist { get; private set; }
        protected string ContainerName { get; private set; }

        public CloudBlobRepository(BlobServiceClient client)
        {
            this.Client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public IBlobRepository Initialize(string containerName, bool createIfNotExist = true)
        {
            if (string.IsNullOrEmpty(containerName))
            {
                throw new ArgumentException($"'{nameof(containerName)}' cannot be null or empty.", nameof(containerName));
            }

            this.ContainerName = containerName;
            this.CreateIfNotExist = createIfNotExist;
            return this;
        }

        public async IAsyncEnumerable<string> ListAsync(string prefix, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            BlobContainerClient container = await GetBlobContainerAsync(cancellationToken);

            AsyncPageable<BlobItem> blobs = container.GetBlobsAsync(prefix: prefix, cancellationToken: cancellationToken);
            await foreach (BlobItem blob in blobs)
            {
                yield return blob.Name;
            }
        }

        public async Task GetAsync(string blobName, Stream destination, CancellationToken cancellationToken = default)
        {
            BlobContainerClient container = await GetBlobContainerAsync(cancellationToken);
            BlobClient blob = container.GetBlobClient(blobName);
            await blob.DownloadToAsync(destination, cancellationToken: cancellationToken);
        }

        public async Task PutAsync(string blobName, Stream stream, bool replaceIfExist = false, IDictionary<string, string> tags = null, CancellationToken cancellationToken = default)
        {
            if (stream.CanSeek)
            {
                stream.Seek(0, SeekOrigin.Begin);
            }

            BlobContainerClient container = await GetBlobContainerAsync(cancellationToken);
            BlobClient blob = container.GetBlobClient(blobName);
            if (replaceIfExist)
            {
                await blob.UploadAsync(stream, overwrite: replaceIfExist, cancellationToken: cancellationToken);
                if (tags != null)
                {
                    await blob.SetTagsAsync(tags, cancellationToken: cancellationToken);
                }
            }
        }

        public async Task DeleteAsync(IEnumerable<string> blobNames, bool ignoreIfNotExist = true, CancellationToken cancellationToken = default)
        {
            BlobContainerClient container = await GetBlobContainerAsync(cancellationToken);
            foreach (string blobName in blobNames)
            {
                if (ignoreIfNotExist)
                {
                    await container.DeleteBlobIfExistsAsync(blobName, cancellationToken: cancellationToken);
                }
                else
                {
                    await container.DeleteBlobAsync(blobName, cancellationToken: cancellationToken);
                }
            }
        }

        protected async Task<BlobContainerClient> GetBlobContainerAsync(CancellationToken cancellationToken)
        {
            BlobContainerClient container = Client.GetBlobContainerClient(ContainerName);
            if (CreateIfNotExist)
            {
                await container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
            }

            return container;
        }
    }
}
