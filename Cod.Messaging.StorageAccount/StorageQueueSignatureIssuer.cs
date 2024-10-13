using Azure.Storage.Queues;
using Azure.Storage.Sas;
using Cod.Platform;

namespace Cod.Messaging.StorageAccount
{
    internal class StorageQueueSignatureIssuer : ISignatureIssuer
    {
        private readonly QueueServiceClient client;

        public StorageQueueSignatureIssuer(QueueServiceClient client)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public bool CanIssue(ResourceType storageType, StorageControl control)
        {
            return storageType == ResourceType.AzureStorageQueue;
        }

        public Task<(string, DateTimeOffset)> IssueAsync(ResourceType storageType, StorageControl control, DateTimeOffset expires, CancellationToken cancellationToken = default)
        {
            QueueClient queue = client.GetQueueClient(control.Resource);
            if (!queue.CanGenerateSasUri)
            {
                throw new NotSupportedException($"Connection string to Queue Endpoint: {client.AccountName} doesn't have a key parameter.");
            }

            MessagingPermissions permissions = (MessagingPermissions)control.Permission;
            QueueSasPermissions internalPermissions = 0;
            if (permissions.HasFlag(MessagingPermissions.Read))
            {
                internalPermissions |= QueueSasPermissions.Read;
            }

            if (permissions.HasFlag(MessagingPermissions.Add))
            {
                internalPermissions |= QueueSasPermissions.Add;
            }

            if (permissions.HasFlag(MessagingPermissions.Update))
            {
                internalPermissions |= QueueSasPermissions.Update;
            }

            if (permissions.HasFlag(MessagingPermissions.ProcessMessages))
            {
                internalPermissions |= QueueSasPermissions.Process;
            }

            return internalPermissions == 0
                ? throw new UnauthorizedAccessException("Cannot create SAS without any permission granted.")
                : Task.FromResult((queue.GenerateSasUri(internalPermissions, expires).ToString(), expires));
        }
    }
}
