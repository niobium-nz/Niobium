using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;

namespace Cod.Platform
{
    public interface ISignatureIssuer
    {
        string Issue(string resource, SharedAccessTablePolicy policy, StorageControl control);

        string Issue(string resource, SharedAccessQueuePolicy policy);
    }
}
