using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Runtime.CompilerServices;

namespace Cod.Storage.Blob
{
    public class CloudBlobRepository : IBlobRepository
    {
        protected BlobServiceClient Client { get; private set; }

        public CloudBlobRepository(BlobServiceClient client)
        {
            Client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public async IAsyncEnumerable<string> GetContainersAsync(string prefix = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            AsyncPageable<BlobContainerItem> containers = Client.GetBlobContainersAsync(prefix: prefix, cancellationToken: cancellationToken);
            await foreach (BlobContainerItem container in containers)
            {
                yield return container.Name;
            }
        }

        public async IAsyncEnumerable<string> ListAsync(string containerName, string prefix = null, bool createIfNotExist = true, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            BlobContainerClient container = await GetBlobContainerAsync(containerName, createIfNotExist, cancellationToken);

            AsyncPageable<BlobItem> blobs = container.GetBlobsAsync(prefix: prefix, cancellationToken: cancellationToken);
            await foreach (BlobItem blob in blobs)
            {
                yield return blob.Name;
            }
        }

        public async Task GetAsync(string containerName, string blobName, Stream destination, bool createIfNotExist = true, CancellationToken cancellationToken = default)
        {
            BlobContainerClient container = await GetBlobContainerAsync(containerName, createIfNotExist, cancellationToken);
            BlobClient blob = container.GetBlobClient(blobName);
            await blob.DownloadToAsync(destination, cancellationToken: cancellationToken);
        }

        public async Task PutAsync(string containerName, string blobName, Stream stream, bool replaceIfExist = false, IDictionary<string, string> tags = null, bool createIfNotExist = true, CancellationToken cancellationToken = default)
        {
            if (stream.CanSeek)
            {
                stream.Seek(0, SeekOrigin.Begin);
            }

            BlobContainerClient container = await GetBlobContainerAsync(containerName, createIfNotExist, cancellationToken);
            BlobClient blob = container.GetBlobClient(blobName);
            await blob.UploadAsync(stream, overwrite: replaceIfExist, cancellationToken: cancellationToken);
            if (tags != null)
            {
                await blob.SetTagsAsync(tags, cancellationToken: cancellationToken);
            }
        }

        public async Task TagAsync(string containerName, string blobName, IDictionary<string, string> tags, CancellationToken cancellationToken = default)
        {
            if (tags.Any())
            {
                BlobContainerClient container = await GetBlobContainerAsync(containerName, false, cancellationToken);
                if (await container.ExistsAsync(cancellationToken))
                {
                    BlobClient blob = container.GetBlobClient(blobName);
                    if (await blob.ExistsAsync(cancellationToken))
                    {
                        await blob.SetTagsAsync(tags, cancellationToken: cancellationToken);
                    }
                }
            }

        }

        public async Task DeleteAsync(string containerName, IEnumerable<string> blobNames, bool ignoreIfNotExist = true, bool createIfNotExist = true, CancellationToken cancellationToken = default)
        {
            BlobContainerClient container = await GetBlobContainerAsync(containerName, createIfNotExist, cancellationToken);
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

        protected async Task<BlobContainerClient> GetBlobContainerAsync(string containerName, bool createIfNotExist, CancellationToken cancellationToken)
        {
            BlobContainerClient container = Client.GetBlobContainerClient(containerName);
            if (createIfNotExist)
            {
                await container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
            }

            return container;
        }
    }
}
