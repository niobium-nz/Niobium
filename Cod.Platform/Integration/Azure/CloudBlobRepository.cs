using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Storage.Blob;

namespace Cod.Platform
{
    internal class CloudBlobRepository : IBlobRepository
    {
        public async Task CreateIfNotExists(string container)
        {
            await CloudStorage.GetBlobContainer(container).CreateIfNotExistsAsync();
        }

        public async Task DeleteAsync(IEnumerable<Uri> blobUris)
        {
            var client = CloudStorage.GetStorageAccount().CreateCloudBlobClient();
            foreach (var item in blobUris)
            {
                var blob = await client.GetBlobReferenceFromServerAsync(item);
                await blob.DeleteIfExistsAsync();
            }
        }

        public async Task<IEnumerable<Uri>> ListAsync(string container, string prefix)
        {
            var c = CloudStorage.GetBlobContainer(container);

            var result = new List<Uri>();
            BlobContinuationToken token = new BlobContinuationToken();
            while (token != null)
            {
                var r = await c.ListBlobsSegmentedAsync(prefix, token);
                result.AddRange(r.Results.Select(b => b.Uri));
                token = r.ContinuationToken;
            }
            return result;
        }

        public async Task PutAsync(string container, string blob, Stream stream, bool replaceIfExist)
        {
            if (stream.CanSeek)
            {
                stream.Seek(0, SeekOrigin.Begin);
            }

            var c = CloudStorage.GetBlobContainer(container);
            await c.CreateIfNotExistsAsync();

            var b = c.GetBlockBlobReference(blob);
            if (!replaceIfExist && await b.ExistsAsync())
            {
                return;
            }
            await b.UploadFromStreamAsync(stream);
        }
    }
}
