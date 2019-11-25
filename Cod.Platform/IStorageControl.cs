using System.Security.Claims;
using System.Threading.Tasks;

namespace Cod.Platform
{
    public interface IStorageControl
    {
        bool Grantable(StorageType type, string resource);

        Task<StorageControl> GrantAsync(ClaimsPrincipal principal, StorageType type, string resource, string partition, string row);
    }
}
