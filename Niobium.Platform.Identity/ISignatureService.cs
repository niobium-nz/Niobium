using System.Security.Claims;

namespace Niobium.Platform.Identity
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
