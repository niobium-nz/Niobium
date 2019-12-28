using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage.Queue;

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
                Microsoft.Azure.Storage.SharedAccessProtocol.HttpsOrHttp,
#else
                Microsoft.Azure.Storage.SharedAccessProtocol.HttpsOnly,
#endif
                null);

        public string Issue(string resource, SharedAccessBlobPolicy policy)
            => CloudStorage.GetBlobContainer(resource).GetSharedAccessSignature(policy, null,
#if DEBUG
                Microsoft.Azure.Storage.SharedAccessProtocol.HttpsOrHttp,
#else
                Microsoft.Azure.Storage.SharedAccessProtocol.HttpsOnly,
#endif
                null);
    }
}
