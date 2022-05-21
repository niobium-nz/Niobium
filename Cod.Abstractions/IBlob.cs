using System.IO;
using System.Threading.Tasks;

namespace Cod
{
    public interface IBlob
    {
        Task<OperationResult> UploadAsync(string container, string path, string contentType, Stream stream);
    }
}
