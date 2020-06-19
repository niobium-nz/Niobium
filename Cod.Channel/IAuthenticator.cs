using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Cod.Channel
{
    public interface IAuthenticator
    {
        AccessToken Token { get; }

        Task InitializeAsync();

        Task<OperationResult<IEnumerable<KeyValuePair<string, string>>>> GetClaimsAsync();

        Task<OperationResult> AquireTokenAsync(string scheme, string username, string password, bool remember);

        Task<OperationResult<StorageSignature>> AquireSignatureAsync(StorageType type, string resource, string partitionKey, string rowKey);

        Task<HttpRequestMessage> PrepareAuthenticationAsync(HttpRequestMessage request);

        Task CleanupAsync();
    }
}
