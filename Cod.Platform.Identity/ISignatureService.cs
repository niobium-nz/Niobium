using System.Security.Claims;

namespace Cod.Platform.Identity
{
    public interface ISignatureService
    {
        Task<StorageSignature> IssueAsync(
            ClaimsPrincipal claims,
            ResourceType type,
            string resource,
            string? partition,
            string? id);
    }
}
