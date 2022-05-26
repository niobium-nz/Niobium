using System.Security.Claims;

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
