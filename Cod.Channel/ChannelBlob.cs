using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Cod.Channel
{
    internal class ChannelBlob : IBlob
    {
        private static readonly IDictionary<string, string> StorageRequestHeaders = new Dictionary<string, string>
        {
            { "x-ms-version", "2019-12-12" },
            { "x-ms-blob-type", "BlockBlob" },
        };

        private readonly IAuthenticator authenticator;
        private readonly IConfigurationProvider configuration;
        private readonly IHttpClient httpClient;

        public ChannelBlob()
        {
        }

        public ChannelBlob(IAuthenticator authenticator, IConfigurationProvider configuration, IHttpClient httpClient)
        {
            this.authenticator = authenticator;
            this.configuration = configuration;
            this.httpClient = httpClient;
        }

        public async Task<OperationResult<StorageSignature>> AquireSignatureAsync(string container)
        {
            return await this.authenticator.AquireSignatureAsync(StorageType.Blob, container, null, null);
        }

        public async Task<OperationResult> Upload(string container, string path, string contentType, Stream stream, string signature = null)
        {
            if (string.IsNullOrEmpty(signature))
            {
                var sig = await this.authenticator.AquireSignatureAsync(StorageType.Blob, container, null, null);
                if (!sig.IsSuccess)
                {
                    return sig;
                }
                signature = sig.Result.Signature;
            }

            var endpoint = await this.configuration.GetSettingAsStringAsync(Constants.KEY_BLOB_URL);
            var url = $"{endpoint}/{container}/{path}{signature}";
            return await this.SendRequest(url, HttpMethod.Put, contentType, stream);
        }

        private async Task<OperationResult> SendRequest(string url, HttpMethod method, string contentType, Stream stream)
        {
            var headers = new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("x-ms-blob-content-type", contentType) };
            foreach (var key in StorageRequestHeaders.Keys)
            {
                headers.Add(new KeyValuePair<string, string>(key, StorageRequestHeaders[key]));
            }

            var result = await this.httpClient.RequestAsync<string>(
                method,
                url,
                body: new StreamContent(stream),
                headers: headers);

            if (!result.IsSuccess)
            {
                return new OperationResult<string>(result);
            }

            return new OperationResult<string>(result.Result);
        }
    }
}
