using System.Security.Claims;
using Microsoft.WindowsAzure.Storage.Table;

namespace Cod.Platform
{
    internal class AzureStorageSignatureService : ISignatureService
    {
        private readonly Lazy<ISignatureIssuer> signatureIssuer;
        private readonly IBlobSignatureIssuer blobSignatureIssuer;
        private readonly IQueueSignatureIssuer queueSignatureIssuer;
        private readonly Lazy<IEnumerable<IStorageControl>> storageControls;

        public AzureStorageSignatureService(
            Lazy<ISignatureIssuer> signatureIssuer,
            IBlobSignatureIssuer blobSignatureIssuer,
            IQueueSignatureIssuer queueSignatureIssuer,
            Lazy<IEnumerable<IStorageControl>> storageControls)
        {
            this.signatureIssuer = signatureIssuer;
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

            string signature;
            try
            {
                signature = storageType switch
                {
                    StorageType.Table => this.signatureIssuer.Value.Issue(
                                            cred.Resource,
                                            new SharedAccessTablePolicy
                                            {
                                                Permissions = (SharedAccessTablePermissions)cred.Permission,
                                                SharedAccessStartTime = start,
                                                SharedAccessExpiryTime = expiry,
                                            },
                                            cred),
                    StorageType.Queue => (await this.queueSignatureIssuer.IssueAsync(
                                            cred.Resource,
                                            expiry,
                                            (QueuePermissions)cred.Permission)).Query,
                    StorageType.Blob => (await this.blobSignatureIssuer.IssueAsync(
                                            cred.Resource,
                                            expiry,
                                            (BlobPermissions)cred.Permission)).Query,
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
                Signature = signature,
                Expiry = expiry.ToUnixTimeSeconds(),
                Control = cred,
            });
        }
    }
}
