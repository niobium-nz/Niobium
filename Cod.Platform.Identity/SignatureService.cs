using System.Security.Claims;

namespace Cod.Platform.Identity
{
    internal class SignatureService(
        Lazy<IEnumerable<ISignatureIssuer>> issuers,
        Lazy<IEnumerable<IResourceControl>> controls)
        : ISignatureService
    {
        public async Task<OperationResult<StorageSignature>> IssueAsync(
            ClaimsPrincipal claims,
            ResourceType type,
            string resource,
            string partition,
            string row)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            DateTimeOffset start = now.AddMinutes(-10);
            DateTimeOffset expiry = now.AddDays(1);
            ResourceType storageType = type;
            IEnumerable<IResourceControl> suitableControls = controls.Value.Where(c => c.Grantable(storageType, resource));
            if (!suitableControls.Any())
            {
                return new OperationResult<StorageSignature>(Cod.InternalError.NotAcceptable);
            }

            StorageControl? cred = null;
            foreach (IResourceControl control in suitableControls)
            {
                cred = await control.GrantAsync(claims, storageType, resource, partition, row);
                if (cred != null)
                {
                    break;
                }
            }

            if (cred == null)
            {
                return new OperationResult<StorageSignature>(Cod.InternalError.Forbidden);
            }

            Uri signatureUri;
            try
            {
                ISignatureIssuer? issuer = issuers.Value.SingleOrDefault(i => i.CanIssue(storageType, cred));
                if (issuer == null)
                {
                    return new OperationResult<StorageSignature>(Cod.InternalError.ServiceUnavailable);
                }

                signatureUri = await issuer.IssueAsync(storageType, cred, expiry);
            }
            catch (UnauthorizedAccessException)
            {
                return new OperationResult<StorageSignature>(Cod.InternalError.Forbidden);
            }

            return new OperationResult<StorageSignature>(new StorageSignature
            {
                Signature = signatureUri.Query,
                Expiry = expiry.ToUnixTimeSeconds(),
                Control = cred,
            });
        }
    }
}
