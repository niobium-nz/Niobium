using System.Security.Claims;

namespace Cod.Platform.Identity.Authorization
{
    internal class AzureStorageSignatureService : ISignatureService
    {
        private readonly Lazy<IEnumerable<ISignatureIssuer>> issuers;
        private readonly Lazy<IEnumerable<IStorageControl>> storageControls;

        public AzureStorageSignatureService(
            Lazy<IEnumerable<ISignatureIssuer>> issuers,
            Lazy<IEnumerable<IStorageControl>> storageControls)
        {
            this.issuers = issuers;
            this.storageControls = storageControls;
        }

        public async Task<OperationResult<StorageSignature>> IssueAsync(
            ClaimsPrincipal claims,
            StorageType type,
            string resource,
            string partition,
            string row)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            DateTimeOffset start = now.AddMinutes(-10);
            DateTimeOffset expiry = now.AddDays(1);
            StorageType storageType = type;
            IEnumerable<IStorageControl> controls = storageControls.Value.Where(c => c.Grantable(storageType, resource));
            if (!controls.Any())
            {
                return new OperationResult<StorageSignature>(Cod.InternalError.NotAcceptable);
            }

            StorageControl cred = null;
            foreach (IStorageControl control in controls)
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
                ISignatureIssuer issuer = issuers.Value.SingleOrDefault(i => i.CanIssue(storageType, cred));
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
