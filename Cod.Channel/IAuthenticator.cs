using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cod.Channel
{
    public interface IAuthenticator
    {
        AccessToken Token { get; set; }

        IReadOnlyDictionary<string, string> Claims { get; }

        Task<OperationResult> AquireTokenAsync(string username, string password);

        Task<OperationResult<StorageSignature>> AquireSignatureAsync(StorageType type, string resource, string partitionKey, string rowKey);
    }
}
