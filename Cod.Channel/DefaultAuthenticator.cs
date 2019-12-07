using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Cod.Contract;
using Newtonsoft.Json;

namespace Cod.Channel
{
    internal class DefaultAuthenticator : IAuthenticator
    {
        private readonly Dictionary<string, StorageSignature> signatures = new Dictionary<string, StorageSignature>();
        private readonly IConfigurationProvider configuration;
        private readonly HttpClient httpClient;

        public event EventHandler AuthenticationRequired;

        public AccessToken Token { get; set; }

        public DefaultAuthenticator(IConfigurationProvider configuration, HttpClient httpClient)
        {
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.httpClient = httpClient;
        }

        public async Task<OperationResult<StorageSignature>> AquireSignatureAsync(StorageType type, string resource, string partitionKey, string rowKey)
        {
            var key = BuildSignatureCacheKey(type, resource, partitionKey, rowKey);
            if (this.signatures.ContainsKey(key))
            {
                if (this.signatures[key].Expires < DateTimeOffset.UtcNow)
                {
                    this.signatures.Remove(key);
                }
                else
                {
                    AuthenticationRequired?.Invoke(this, EventArgs.Empty);
                    return OperationResult<StorageSignature>.Create(this.signatures[key]);
                }
            }

            if (this.Token == null || this.Token.Expires < DateTimeOffset.UtcNow)
            {
                return OperationResult<StorageSignature>.Create(InternalError.Unauthenticated, null);
            }

            var apiUrl = await this.configuration.GetSettingAsync(Constants.KEY_API_URL);
            var path = new StringBuilder($"/v1/signature/{(int)type}/{resource}");
            if (!String.IsNullOrWhiteSpace(partitionKey))
            {
                path.Append("/");
                path.Append(partitionKey);
            }

            if (!String.IsNullOrWhiteSpace(rowKey))
            {
                path.Append("/");
                path.Append(rowKey);
            }

            var request = new HttpRequestMessage(HttpMethod.Get, $"{apiUrl}{path}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", this.Token.Token);
            var response = await this.httpClient.SendAsync(request);
            var statusCode = (int)response.StatusCode;
            if (statusCode >= 200 && statusCode < 300)
            {
                var json = await response.Content.ReadAsStringAsync();
                var signature = JsonConvert.DeserializeObject<StorageSignature>(json);
                this.signatures.Add(key, signature);
                return OperationResult<StorageSignature>.Create(signature);
            }
            return OperationResult<StorageSignature>.Create(statusCode, null); // TODO (5he11) 从body中解出来自定义错误代码，而不是HTTP响应代码
        }

        public async Task<OperationResult> AquireTokenAsync(string username, string password)
        {
            var apiUrl = await this.configuration.GetSettingAsync(Constants.KEY_API_URL);
            var creds = Encoding.ASCII.GetBytes($"{username.Trim()}:{password.Trim()}");
            var request = new HttpRequestMessage(HttpMethod.Get, $"{apiUrl}/v1/token");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(creds));
            var response = await this.httpClient.SendAsync(request);
            var statusCode = (int)response.StatusCode;
            if (statusCode >= 200 && statusCode < 300)
            {
                var header = response.Headers.WwwAuthenticate.SingleOrDefault();
                if (header != null && header.Scheme == "Bearer")
                {
                    this.Token = new AccessToken
                    {
                        Token = header.Parameter,
                        Expiry = DateTimeOffset.UtcNow.AddHours(8).ToUnixTimeSeconds(), // TODO (5he11) 从JWT中解码得到，而不是硬编码
                    };
                    return OperationResult.Create();
                }
            }
            return OperationResult.Create(statusCode); // TODO (5he11) 从body中解出来自定义错误代码，而不是HTTP响应代码
        }

        private static string BuildSignatureCacheKey(StorageType type, string resource, string partitionKey, string rowKey)
            => $"{(int)type}|{resource}|{partitionKey}|{rowKey}";
    }
}
