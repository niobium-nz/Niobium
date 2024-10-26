using System.Text;

namespace Cod.File
{
    public static class IFileServiceExtensions
    {
        public async static Task PutAsync(this IFileService fileService, string partition, string filename, string content, bool replaceIfExist = false, IDictionary<string, string>? tags = null, bool createIfNotExist = true, CancellationToken cancellationToken = default)
            => await fileService.PutAsync(partition, filename, Encoding.UTF8.GetBytes(content), replaceIfExist, tags, createIfNotExist, cancellationToken);

        public async static Task PutAsync(this IFileService fileService, string partition, string filename, byte[] buff, bool replaceIfExist = false, IDictionary<string, string>? tags = null, bool createIfNotExist = true, CancellationToken cancellationToken = default)
        {
            using (var ms = new MemoryStream(buff))
            {
                await fileService.PutAsync(partition, filename, ms, replaceIfExist, tags, createIfNotExist, cancellationToken);
            }
        }
    }
}
