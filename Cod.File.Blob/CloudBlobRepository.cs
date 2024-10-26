using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Runtime.CompilerServices;

namespace Cod.File.Blob
{
    internal class CloudBlobRepository(AzureBlobClientFactory clientFactory) : IFileService
    {
        public async IAsyncEnumerable<string> GetPartitionsAsync(string? prefix = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var client = await clientFactory.CreateClientAsync(cancellationToken);
            AsyncPageable<BlobContainerItem> containers = client.GetBlobContainersAsync(prefix: prefix, cancellationToken: cancellationToken);
            await foreach (BlobContainerItem container in containers)
            {
                yield return container.Name;
            }
        }

        public async IAsyncEnumerable<string> ListAsync(string partition, string? prefix = null, bool createIfNotExist = true, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            BlobContainerClient container = await GetBlobContainerAsync(FilePermissions.List, partition, createIfNotExist, cancellationToken);

            AsyncPageable<BlobItem> blobs = container.GetBlobsAsync(prefix: prefix, cancellationToken: cancellationToken);
            await foreach (BlobItem blob in blobs)
            {
                yield return blob.Name;
            }
        }

        public async Task<Stream?> GetAsync(string partition, string filename, bool createIfNotExist = true, CancellationToken cancellationToken = default)
        {
            BlobContainerClient container = await GetBlobContainerAsync(FilePermissions.Read, partition, createIfNotExist, cancellationToken);
            BlobClient blob = container.GetBlobClient(filename);
            var response = await blob.DownloadStreamingAsync(cancellationToken: cancellationToken);
            if (!response.HasValue)
            {
                return null;
            }

            return response.Value.Content;
        }

        public async Task PutAsync(string partition, string filename, Stream stream, bool replaceIfExist = false, IDictionary<string, string>? tags = null, bool createIfNotExist = true, CancellationToken cancellationToken = default)
        {
            if (stream.CanSeek)
            {
                stream.Seek(0, SeekOrigin.Begin);
            }

            BlobContainerClient container = await GetBlobContainerAsync(FilePermissions.Write, partition, createIfNotExist, cancellationToken);
            BlobClient blob = container.GetBlobClient(filename);
            await blob.UploadAsync(stream, overwrite: replaceIfExist, cancellationToken: cancellationToken);
            if (tags != null)
            {
                await blob.SetTagsAsync(tags, cancellationToken: cancellationToken);
            }
        }

        public async Task TagAsync(string partition, string filename, IDictionary<string, string> tags, CancellationToken cancellationToken = default)
        {
            if (tags.Any())
            {
                BlobContainerClient container = await GetBlobContainerAsync(FilePermissions.Write, partition, false, cancellationToken);
                if (await container.ExistsAsync(cancellationToken))
                {
                    BlobClient blob = container.GetBlobClient(filename);
                    if (await blob.ExistsAsync(cancellationToken))
                    {
                        await blob.SetTagsAsync(tags, cancellationToken: cancellationToken);
                    }
                }
            }

        }

        public async Task DeleteAsync(string partition, IEnumerable<string> filename, bool ignoreIfNotExist = true, bool createIfNotExist = true, CancellationToken cancellationToken = default)
        {
            BlobContainerClient container = await GetBlobContainerAsync(FilePermissions.Delete, partition, createIfNotExist, cancellationToken);
            foreach (string blobName in filename)
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

        protected async Task<BlobContainerClient> GetBlobContainerAsync(FilePermissions permission, string partition, bool createIfNotExist, CancellationToken cancellationToken)
        {
            var client = await clientFactory.CreateClientAsync([permission], partition, cancellationToken);
            BlobContainerClient container = client.GetBlobContainerClient(partition);
            if (createIfNotExist)
            {
                await container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
            }

            return container;
        }
    }
}
