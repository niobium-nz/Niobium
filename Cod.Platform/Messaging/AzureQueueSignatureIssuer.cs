using Azure.Storage.Queues;
using Azure.Storage.Sas;

namespace Cod.Platform.Messaging
{
    internal class AzureQueueSignatureIssuer : IQueueSignatureIssuer
    {
        private readonly QueueServiceClient client;

        public AzureQueueSignatureIssuer(QueueServiceClient client)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public Task<Uri> IssueAsync(string queueName, DateTimeOffset expires, QueuePermissions permissions, CancellationToken cancellationToken = default)
        {
            QueueClient queue = client.GetQueueClient(queueName);
            if (!queue.CanGenerateSasUri)
            {
                throw new NotSupportedException($"Connection string to Queue Endpoint: {client.AccountName} doesn't have a key parameter.");
            }

            QueueSasPermissions internalPermissions = 0;
            if (permissions.HasFlag(QueuePermissions.Read))
            {
                internalPermissions |= QueueSasPermissions.Read;
            }

            if (permissions.HasFlag(QueuePermissions.Add))
            {
                internalPermissions |= QueueSasPermissions.Add;
            }

            if (permissions.HasFlag(QueuePermissions.Update))
            {
                internalPermissions |= QueueSasPermissions.Update;
            }

            if (permissions.HasFlag(QueuePermissions.ProcessMessages))
            {
                internalPermissions |= QueueSasPermissions.Process;
            }

            return internalPermissions == 0
                ? throw new InvalidOperationException("Cannot create SAS without any permission granted.")
                : Task.FromResult(queue.GenerateSasUri(internalPermissions, expires));
        }
    }
}
