using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;

namespace Cod.Platform
{
    internal class CloudSignatureIssuer : ISignatureIssuer
    {
        public string Issue(string resource, SharedAccessTablePolicy policy, StorageControl control)
            => CloudStorage.GetTable(resource).GetSharedAccessSignature(policy, null,
                control.StartPartitionKey, control.StartRowKey, control.EndPartitionKey, control.EndRowKey,
#if DEBUG
                SharedAccessProtocol.HttpsOrHttp,
#else
                SharedAccessProtocol.HttpsOnly,
#endif
                null);

        public string Issue(string resource, SharedAccessQueuePolicy policy)
            => CloudStorage.GetQueue(resource).GetSharedAccessSignature(policy, null,
#if DEBUG
                SharedAccessProtocol.HttpsOrHttp,
#else
                SharedAccessProtocol.HttpsOnly,
#endif
                null);

        public string Issue(string resource, SharedAccessBlobPolicy policy)
            => CloudStorage.GetBlobContainer(resource).GetSharedAccessSignature(policy, null,
#if DEBUG
                SharedAccessProtocol.HttpsOrHttp,
#else
                SharedAccessProtocol.HttpsOnly,
#endif
                null);
    }
}
