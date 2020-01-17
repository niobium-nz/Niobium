using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Blazor.Extensions.Storage.Interfaces;
using Microsoft.IdentityModel.JsonWebTokens;
using Newtonsoft.Json;

namespace Cod.Channel
{
    internal class DefaultAuthenticator : IAuthenticator
    {
        private readonly Dictionary<string, StorageSignature> signatures = new Dictionary<string, StorageSignature>();
        private readonly IConfigurationProvider configuration;
        private readonly HttpClient httpClient;
        private readonly ISessionStorage sessionStorage;
        private readonly IEnumerable<IEventHandler<IAuthenticator>> eventHandlers;
        private readonly Dictionary<string, string> claims;

        public event EventHandler AuthenticationRequired;

        public async Task<IReadOnlyDictionary<string, string>> GetClaimsAsync()
        {
            if (!this.IsAuthenticated())
            {
                await this.CleanupAsync();
            }
            return this.claims;
        }

        public AccessToken Token { get; private set; }

        public DefaultAuthenticator(IConfigurationProvider configuration,
            HttpClient httpClient,
            ISessionStorage sessionStorage,
            IEnumerable<IEventHandler<IAuthenticator>> eventHandlers)
        {
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.httpClient = httpClient;
            this.sessionStorage = sessionStorage;
            this.eventHandlers = eventHandlers;
            this.claims = new Dictionary<string, string>();
        }

        public async Task InitializeAsync()
        {
            var token = await this.sessionStorage.GetItem<string>("accessToken");
            if (!String.IsNullOrWhiteSpace(token))
            {
                this.SetToken(token);
            }
            var ss = await this.sessionStorage.GetItem<Dictionary<string, StorageSignature>>("signatures");
            if (ss != null && ss.Count > 0)
            {
                foreach (var key in ss.Keys)
                {
                    this.signatures.Add(key, ss[key]);
                }
            }
        }

        public async Task<OperationResult<StorageSignature>> AquireSignatureAsync(StorageType type, string resource, string partitionKey, string rowKey)
        {
            if (!this.IsAuthenticated())
            {
                await this.CleanupAsync();
                return OperationResult<StorageSignature>.Create(InternalError.AuthenticationRequired, null);
            }

            var key = BuildSignatureCacheKey(this.Token.Token, type, resource, partitionKey, rowKey);
            if (this.signatures.ContainsKey(key))
            {
                if (this.signatures[key].Expires < DateTimeOffset.UtcNow)
                {
                    this.signatures.Remove(key);
                    await this.SaveSignaturesAsync();
                }
                else
                {
                    AuthenticationRequired?.Invoke(this, EventArgs.Empty);
                    return OperationResult<StorageSignature>.Create(this.signatures[key]);
                }
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
                await this.SaveSignaturesAsync();
                return OperationResult<StorageSignature>.Create(signature);
            }
            else if (statusCode == 401)
            {
                this.Token = null;
                this.claims.Clear();
                this.signatures.Clear();
                foreach (var eventHandler in this.eventHandlers)
                {
                    await eventHandler.InvokeAsync(this);
                }
                await this.SaveSignaturesAsync();
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

        public async Task<OperationResult> AquireTokenAsync(string username, string password, bool remember)
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
                    this.SetToken(header.Parameter);
                    if (remember)
                    {
                        await this.sessionStorage.SetItem("accessToken", header.Parameter);
                    }
                    return OperationResult.Create();
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

        private void SetToken(string token)
        {
            try
            {
                var jwt = new JsonWebToken(token);
                var dic = jwt.Claims.ToDictionary(c => c.Type, c => c.Value);
                foreach (var key in dic.Keys)
                {
                    this.claims.Add(key, dic[key]);
                }
                this.Token = new AccessToken
                {
                    Token = token,
                    Expiry = new DateTimeOffset(jwt.ValidTo).ToUnixTimeSeconds(),
                };
            }
            catch (ArgumentException)
            {
            }
        }

        private async Task CleanupAsync()
        {
            this.Token = null;
            this.claims.Clear();
            this.signatures.Clear();
            foreach (var eventHandler in this.eventHandlers)
            {
                await eventHandler.InvokeAsync(this);
            }
            await this.SaveSignaturesAsync();
        }

        private Task SaveSignaturesAsync() => Task.CompletedTask;// await this.sessionStorage.SetItem("signatures", this.signatures);

        private static string BuildSignatureCacheKey(string token, StorageType type, string resource, string partitionKey, string rowKey)
            => $"{token}|{(int)type}|{resource}|{partitionKey}|{rowKey}";
    }
}
