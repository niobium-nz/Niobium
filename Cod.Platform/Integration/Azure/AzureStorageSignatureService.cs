using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage.Queue;

namespace Cod.Platform
{
    internal class AzureStorageSignatureService : ISignatureService
    {
        private readonly Lazy<ISignatureIssuer> signatureIssuer;
        private readonly Lazy<IEnumerable<IStorageControl>> storageControls;

        public AzureStorageSignatureService(
            Lazy<ISignatureIssuer> signatureIssuer,
            Lazy<IEnumerable<IStorageControl>> storageControls)
        {
            this.signatureIssuer = signatureIssuer;
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
                    StorageType.Queue => this.signatureIssuer.Value.Issue(
                                            cred.Resource,
                                            new SharedAccessQueuePolicy
                                            {
                                                Permissions = (SharedAccessQueuePermissions)cred.Permission,
                                                SharedAccessStartTime = start,
                                                SharedAccessExpiryTime = expiry,
                                            }),
                    StorageType.Blob => this.signatureIssuer.Value.Issue(
                                            cred.Resource,
                                            new SharedAccessBlobPolicy
                                            {
                                                Permissions = (SharedAccessBlobPermissions)cred.Permission,
                                                SharedAccessStartTime = start,
                                                SharedAccessExpiryTime = expiry,
                                            }),
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
