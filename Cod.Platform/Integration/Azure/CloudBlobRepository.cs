using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using System.Runtime.CompilerServices;

namespace Cod.Platform.Integration.Azure
{
    internal class CloudBlobRepository : IBlobRepository
    {
        private readonly BlobServiceClient client;
        private bool createIfNotExist;
        private string containerName;

        public CloudBlobRepository(BlobServiceClient client)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public IBlobRepository Initialize(string containerName, bool createIfNotExist = true)
        {
            if (string.IsNullOrEmpty(containerName))
            {
                throw new ArgumentException($"'{nameof(containerName)}' cannot be null or empty.", nameof(containerName));
            }

            this.containerName = containerName;
            this.createIfNotExist = createIfNotExist;
            return this;
        }

        public async IAsyncEnumerable<string> ListAsync(string prefix, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            BlobContainerClient container = await GetBlobContainerAsync(cancellationToken);

            var blobs = container.GetBlobsAsync(prefix: prefix, cancellationToken: cancellationToken);
            await foreach (var blob in blobs)
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

        private async Task<BlobContainerClient> GetBlobContainerAsync(CancellationToken cancellationToken)
        {
            BlobContainerClient container = client.GetBlobContainerClient(containerName);
            if (createIfNotExist)
            {
                await container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
            }

            return container;
        }

        public Task PutAsync(string blobName, Stream stream, bool replaceIfExist = false, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
