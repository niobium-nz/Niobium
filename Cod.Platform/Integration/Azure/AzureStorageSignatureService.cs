using System.Security.Claims;

namespace Cod.Platform
{
    internal class AzureStorageSignatureService : ISignatureService
    {
        private readonly ITableSignatureIssuer tableSignatureIssuer;
        private readonly IBlobSignatureIssuer blobSignatureIssuer;
        private readonly IQueueSignatureIssuer queueSignatureIssuer;
        private readonly Lazy<IEnumerable<IStorageControl>> storageControls;

        public AzureStorageSignatureService(
            ITableSignatureIssuer tableSignatureIssuer,
            IBlobSignatureIssuer blobSignatureIssuer,
            IQueueSignatureIssuer queueSignatureIssuer,
            Lazy<IEnumerable<IStorageControl>> storageControls)
        {
            this.tableSignatureIssuer = tableSignatureIssuer;
            this.blobSignatureIssuer = blobSignatureIssuer;
            this.queueSignatureIssuer = queueSignatureIssuer;
            this.storageControls = storageControls;
        }

        public async Task<OperationResult<StorageSignature>> IssueAsync(
            ClaimsPrincipal claims,
            StorageType type,
            string resource,
            string partition,
            string row)
        {
            var now = DateTimeOffset.UtcNow;
            var start = now.AddMinutes(-10);
            var expiry = now.AddDays(1);
            var storageType = type;
            var controls = this.storageControls.Value.Where(c => c.Grantable(storageType, resource));
            if (!controls.Any())
            {
                return new OperationResult<StorageSignature>(InternalError.NotAcceptable);
            }

            StorageControl cred = null;
            foreach (var control in controls)
            {
                cred = await control.GrantAsync(claims, storageType, resource, partition, row);
                if (cred != null)
                {
                    break;
                }
            }

            if (cred == null)
            {
                return new OperationResult<StorageSignature>(InternalError.Forbidden);
            }

            Uri signatureUri;
            try
            {
                signatureUri = storageType switch
                {
                    StorageType.Table => await this.tableSignatureIssuer.IssueAsync(expiry, cred),
                    StorageType.Queue => await this.queueSignatureIssuer.IssueAsync(cred.Resource, expiry, (QueuePermissions)cred.Permission),
                    StorageType.Blob => await this.blobSignatureIssuer.IssueAsync(cred.Resource, expiry, (BlobPermissions)cred.Permission),
                    StorageType.File => throw new NotImplementedException(),
                    _ => throw new NotImplementedException(),
                };
            }
            catch (UnauthorizedAccessException)
            {
                return new OperationResult<StorageSignature>(InternalError.Forbidden);
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
