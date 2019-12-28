using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage.Queue;

namespace Cod.Platform
{
    public interface ISignatureIssuer
    {
        string Issue(string resource, SharedAccessTablePolicy policy, StorageControl control);

        string Issue(string resource, SharedAccessQueuePolicy policy);

        string Issue(string resource, SharedAccessBlobPolicy policy);
    }
}
