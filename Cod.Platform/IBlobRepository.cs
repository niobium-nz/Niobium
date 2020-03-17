using System.IO;
using System.Threading.Tasks;

namespace Cod.Platform
{
    public interface IBlobRepository
    {
        Task PutAsync(string container, string blob, Stream stream, bool replaceIfExist);
    }
}
