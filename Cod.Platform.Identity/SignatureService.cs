using System.Security.Claims;

namespace Cod.Platform.Identity
{
    internal sealed class SignatureService(
        Lazy<IEnumerable<ISignatureIssuer>> issuers,
        Lazy<IEnumerable<IResourceControl>> controls)
        : ISignatureService
    {
        public async Task<StorageSignature> IssueAsync(
            ClaimsPrincipal claims,
            ResourceType type,
            string resource,
            string? partition,
            string? id)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            DateTimeOffset start = now.AddMinutes(-10);
            DateTimeOffset expiry = now.AddDays(1);
            IEnumerable<IResourceControl> suitableControls = controls.Value.Where(c => c.Grantable(type, resource));
            if (!suitableControls.Any())
            {
                throw new ApplicationException(InternalError.NotAcceptable);
            }

            StorageControl? cred = null;
            foreach (IResourceControl control in suitableControls)
            {
                cred = await control.GrantAsync(claims, type, resource, partition, id);
                if (cred != null)
                {
                    break;
                }
            }

            if (cred == null)
            {
                throw new ApplicationException(InternalError.Forbidden);
            }

            string signature;
            DateTimeOffset exp;
            try
            {
                ISignatureIssuer? issuer = issuers.Value.SingleOrDefault(i => i.CanIssue(type, cred))
                    ?? throw new ApplicationException(InternalError.ServiceUnavailable);
                (signature, exp) = await issuer.IssueAsync(type, cred, expiry);
            }
            catch (UnauthorizedAccessException)
            {
                throw new ApplicationException(InternalError.Forbidden);
            }

            return new StorageSignature
            {
                Signature = signature,
                Expiry = exp.ToUnixTimeSeconds(),
                Control = cred,
            };
        }
    }
}
