using Microsoft.WindowsAzure.Storage.Table;

namespace Cod.Platform
{
    public interface ISignatureIssuer
    {
        string Issue(string resource, SharedAccessTablePolicy policy, StorageControl control);
    }
}
