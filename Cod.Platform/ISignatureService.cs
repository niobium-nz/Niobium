using System.Security.Claims;
using System.Threading.Tasks;

namespace Cod.Platform
{
    public interface ISignatureService
    {
        Task<OperationResult<StorageSignature>> IssueAsync(
            ClaimsPrincipal claims,
            StorageType type,
            string resource,
            string partition,
            string row);
    }
}
