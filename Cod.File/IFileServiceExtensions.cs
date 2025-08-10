using System.Text;

namespace Cod.File
{
    public static class IFileServiceExtensions
    {
        public static async Task<string?> GetAsStringAsync(this IFileService fileService, string partition, string filename, bool createIfNotExist = true, CancellationToken cancellationToken = default)
        {
            Stream? stream = await fileService.GetAsync(partition, filename, createIfNotExist, cancellationToken);
            if (stream == null)
            {
                return null;
            }

            using (stream)
            using (StreamReader reader = new(stream, Encoding.UTF8))
            {
                return await reader.ReadToEndAsync(cancellationToken);
            }
        }

        public static async Task PutAsync(this IFileService fileService, string partition, string filename, string content, bool replaceIfExist = false, IDictionary<string, string>? tags = null, bool createIfNotExist = true, CancellationToken cancellationToken = default)
        {
            await fileService.PutAsync(partition, filename, Encoding.UTF8.GetBytes(content), replaceIfExist, tags, createIfNotExist, cancellationToken);
        }

        public static async Task PutAsync(this IFileService fileService, string partition, string filename, byte[] buff, bool replaceIfExist = false, IDictionary<string, string>? tags = null, bool createIfNotExist = true, CancellationToken cancellationToken = default)
        {
            using MemoryStream ms = new(buff);
            await fileService.PutAsync(partition, filename, ms, replaceIfExist, tags, createIfNotExist, cancellationToken);
        }
    }
}
