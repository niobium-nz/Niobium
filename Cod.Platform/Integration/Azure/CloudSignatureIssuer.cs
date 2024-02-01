using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace Cod.Platform.Integration.Azure
{
    internal class CloudSignatureIssuer : ISignatureIssuer
    {
        public string Issue(string resource, SharedAccessTablePolicy policy, StorageControl control)
        {
            return CloudStorage.GetTable(resource).GetSharedAccessSignature(policy, null,
                        control.StartPartitionKey, control.StartRowKey, control.EndPartitionKey, control.EndRowKey,
                        SharedAccessProtocol.HttpsOrHttp, null);
        }
    }
}
