using System.Security.Claims;

namespace Cod.Platform.Identity
{
    public interface ISignatureService
    {
        Task<OperationResult<StorageSignature>> IssueAsync(
            ClaimsPrincipal claims,
            ResourceType type,
            string resource,
            string partition,
            string row);
    }
}
