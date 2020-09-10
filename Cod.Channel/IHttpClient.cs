using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Cod.Channel
{
    public interface IHttpClient
    {
        Task<OperationResult<T>> RequestAsync<T>(
           HttpMethod method,
           string uri,
           string bearerToken = null,
           object body = null,
           IEnumerable<KeyValuePair<string, string>> headers = null,
           string contentType = null);

        Task<OperationResult<HttpResponseMessage>> RequestAsync(
            HttpMethod method,
            string uri,
            string bearerToken = null,
            object body = null,
            IEnumerable<KeyValuePair<string, string>> headers = null,
            string contentType = null);
    }
}
