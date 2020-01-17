using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Cod.Channel
{
    public interface IAuthenticator
    {
        AccessToken Token { get; }

        Task<IReadOnlyDictionary<string, string>> GetClaimsAsync();

        Task<OperationResult> AquireTokenAsync(string username, string password, bool remember);

        Task<OperationResult<StorageSignature>> AquireSignatureAsync(StorageType type, string resource, string partitionKey, string rowKey);

        Task<HttpRequestMessage> PrepareAuthenticationAsync(HttpRequestMessage request);
    }
}
