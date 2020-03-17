using System.IO;
using System.Threading.Tasks;

namespace Cod.Platform
{
    internal class CloudBlobRepository : IBlobRepository
    {
        public async Task PutAsync(string container, string blob, Stream stream, bool replaceIfExist)
        {
            var b = CloudStorage.GetBlob(container, blob);
            if (!replaceIfExist && await b.ExistsAsync())
            {
                return;
            }
            await b.UploadFromStreamAsync(stream);
        }
    }
}
