using System;
using System.Threading.Tasks;
using Cod.Contract;

namespace Cod.Channel
{
    public interface IAuthenticator
    {
        AccessToken Token { get; set; }

        event EventHandler AuthenticationRequired;

        Task<OperationResult> AquireTokenAsync(string username, string password);

        Task<OperationResult<StorageSignature>> AquireSignatureAsync(StorageType type, string resource, string partitionKey, string rowKey);
    }
}
