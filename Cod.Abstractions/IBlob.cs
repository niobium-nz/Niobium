using System.IO;
using System.Threading.Tasks;

namespace Cod
{
    public interface IBlob
    {
        Task<OperationResult<StorageSignature>> AquireSignatureAsync(string container);

        Task<OperationResult> Upload(string container, string path, string contentType, Stream stream, string signature = null);
    }
}
