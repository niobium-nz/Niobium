using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.JsonWebTokens;
using Newtonsoft.Json;

namespace Cod.Channel
{
    internal class DefaultAuthenticator : IAuthenticator
    {
        private readonly Dictionary<string, StorageSignature> signatures = new Dictionary<string, StorageSignature>();
        private readonly IConfigurationProvider configuration;
        private readonly HttpClient httpClient;
        private readonly IEnumerable<IEventHandler<IAuthenticator>> eventHandlers;

        public event EventHandler AuthenticationRequired;

        public IReadOnlyDictionary<string, string> Claims { get; private set; } = new Dictionary<string, string>();

        public AccessToken Token { get; set; }

        public DefaultAuthenticator(IConfigurationProvider configuration,
            HttpClient httpClient,
            IEnumerable<IEventHandler<IAuthenticator>> eventHandlers)
        {
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.httpClient = httpClient;
            this.eventHandlers = eventHandlers;
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

            if (!this.IsAuthenticated())
            {
                this.Token = null;
                this.Claims = new Dictionary<string, string>();
                this.signatures.Clear();
                foreach (var eventHandler in this.eventHandlers)
                {
                    await eventHandler.HandleAsync(this);
                }
                return OperationResult<StorageSignature>.Create(InternalError.AuthenticationRequired, null);
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
            else if (statusCode == 401)
            {
                this.Token = null;
                this.Claims = new Dictionary<string, string>();
                this.signatures.Clear();
                foreach (var eventHandler in this.eventHandlers)
                {
                    await eventHandler.HandleAsync(this);
                }
                return OperationResult<StorageSignature>.Create(InternalError.AuthenticationRequired, null);
            }
            else if (InternalError.Messages.ContainsKey(statusCode))
            {
                return OperationResult<StorageSignature>.Create(statusCode, null);
            }
            else
            {
                return OperationResult<StorageSignature>.Create(InternalError.Unknown, null);
            }
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
                    try
                    {
                        var jwt = new JsonWebToken(header.Parameter);
                        this.Claims = jwt.Claims.ToDictionary(c => c.Type, c => c.Value);
                        this.Token = new AccessToken
                        {
                            Token = header.Parameter,
                            Expiry = new DateTimeOffset(jwt.ValidTo).ToUnixTimeSeconds(),
                        };
                        return OperationResult.Create();
                    }
                    catch (ArgumentException)
                    {
                    }
                }

                return OperationResult.Create(InternalError.AuthenticationRequired);
            }
            else if (InternalError.Messages.ContainsKey(statusCode))
            {
                return OperationResult.Create(statusCode);
            }
            else
            {
                return OperationResult.Create(InternalError.Unknown);
            }
        }

        private static string BuildSignatureCacheKey(StorageType type, string resource, string partitionKey, string rowKey)
            => $"{(int)type}|{resource}|{partitionKey}|{rowKey}";
    }
}
