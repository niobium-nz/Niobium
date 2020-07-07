using System.IO;
using System.Threading.Tasks;

namespace Cod.Platform
{
    public interface IBlobRepository
    {
        Task CreateIfNotExists(string container);

        Task PutAsync(string container, string blob, Stream stream, bool replaceIfExist);
    }
}
