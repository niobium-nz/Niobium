using System;
using System.Collections.Concurrent;
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
    public class DefaultAuthenticator : IAuthenticator
    {
        private static string savedToken;

        private readonly ConcurrentDictionary<string, StorageSignature> signatures = new ConcurrentDictionary<string, StorageSignature>();
        private readonly IConfigurationProvider configuration;
        private readonly HttpClient httpClient;
        private readonly IEnumerable<IEventHandler<IAuthenticator>> eventHandlers;
        private readonly ConcurrentBag<KeyValuePair<string, string>> claims;

        public event EventHandler AuthenticationRequired;

        public async Task<OperationResult<IEnumerable<KeyValuePair<string, string>>>> GetClaimsAsync()
        {
            if (!this.IsAuthenticated())
            {
                await this.CleanupAsync();
                return OperationResult<IEnumerable<KeyValuePair<string, string>>>.Create(InternalError.AuthenticationRequired, null);
            }
            return OperationResult<IEnumerable<KeyValuePair<string, string>>>.Create(this.claims);
        }

        public AccessToken Token { get; private set; }

        public DefaultAuthenticator(IConfigurationProvider configuration,
            HttpClient httpClient,
            IEnumerable<IEventHandler<IAuthenticator>> eventHandlers)
        {
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.httpClient = httpClient;
            this.eventHandlers = eventHandlers;
            this.claims = new ConcurrentBag<KeyValuePair<string, string>>();
        }

        protected virtual Task<string> GetSavedTokenAsync() => String.IsNullOrWhiteSpace(savedToken) ? Task.FromResult(String.Empty) : Task.FromResult(savedToken);

        protected virtual Task SaveTokenAsync(string token)
        {
            savedToken = token;
            return Task.CompletedTask;
        }

        protected virtual Task<IDictionary<string, StorageSignature>> GetSavedSignaturesAsync() => Task.FromResult<IDictionary<string, StorageSignature>>(null);

        protected virtual Task SaveSignaturesAsync(IDictionary<string, StorageSignature> signatures) => Task.CompletedTask;

        public virtual async Task InitializeAsync()
        {
            var token = await this.GetSavedTokenAsync();
            System.Console.WriteLine($"GetSavedTokenAsync -> {token}");
            if (!String.IsNullOrWhiteSpace(token))
            {
                await this.SetTokenAsync(token);
                await this.SaveTokenAsync(token);
            }
            System.Console.WriteLine($"Validate -> {this.Token.Validate()}");
            var ss = await this.GetSavedSignaturesAsync();
            if (ss != null && ss.Count > 0)
            {
                foreach (var key in ss.Keys)
                {
                    this.signatures.AddOrUpdate(key, k => ss[key], (k, v) => ss[key]);
                }
            }
        }

        public virtual async Task<OperationResult<StorageSignature>> AquireSignatureAsync(StorageType type, string resource, string partitionKey, string rowKey)
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
                    this.signatures.TryRemove(key, out _);
                    await this.SaveSignaturesAsync(this.signatures);
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
                this.signatures.AddOrUpdate(key, k => signature, (k, v) => signature);
                await this.SaveSignaturesAsync(this.signatures);
                return OperationResult<StorageSignature>.Create(signature);
            }
            else if (statusCode == 401)
            {
                await this.CleanupAsync();
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

        public virtual async Task<OperationResult> AquireTokenAsync(string scheme, string username, string password, bool remember)
        {
            var apiUrl = await this.configuration.GetSettingAsync(Constants.KEY_API_URL);
            var creds = Encoding.ASCII.GetBytes($"{username.Trim()}:{password.Trim()}");
            var request = new HttpRequestMessage(HttpMethod.Get, $"{apiUrl}/v1/token");
            request.Headers.Authorization = new AuthenticationHeaderValue(scheme, Convert.ToBase64String(creds));
            var response = await this.httpClient.SendAsync(request);
            var statusCode = (int)response.StatusCode;
            if (statusCode >= 200 && statusCode < 300)
            {
                var header = response.Headers.WwwAuthenticate.SingleOrDefault();
                if (header != null && header.Scheme == "Bearer")
                {
                    await this.SetTokenAsync(header.Parameter);
                    if (remember)
                    {
                        await this.SaveTokenAsync(header.Parameter);
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

        public async Task CleanupAsync()
        {
            await CleanupCredentialsAsync();
            foreach (var eventHandler in this.eventHandlers)
            {
                await eventHandler.InvokeAsync(this);
            }
        }

        public async Task<HttpRequestMessage> PrepareAuthenticationAsync(HttpRequestMessage request)
        {
            if (!this.IsAuthenticated())
            {
                await this.CleanupAsync();
            }
            else
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", this.Token.Token);
            }

            return request;
        }

        protected async Task SetTokenAsync(string token)
        {
            await this.CleanupCredentialsAsync();
            try
            {
                var jwt = new JsonWebToken(token);
                var pairs = jwt.Claims.Select(c => new KeyValuePair<string, string>(c.Type, c.Value));
                foreach (var pair in pairs)
                {
                    this.claims.Add(pair);
                }
                this.Token = new AccessToken
                {
                    Token = token,
                    Expiry = new DateTimeOffset(jwt.ValidTo).ToUnixTimeSeconds(),
                };
                System.Console.WriteLine($"this.Token -> {this.Token.Token} -> {this.Token.Expiry}");
            }
            catch (ArgumentException)
            {
                System.Console.WriteLine($"ArgumentException");
            }
        }

        private async Task CleanupCredentialsAsync()
        {
            this.Token = null;
            while (this.claims.Count > 0)
            {
                this.claims.TryTake(out _);
            }

            await this.SaveTokenAsync(string.Empty);
            this.signatures.Clear();
            savedToken = null;
            await this.SaveSignaturesAsync(this.signatures);
        }

        private static string BuildSignatureCacheKey(string token, StorageType type, string resource, string partitionKey, string rowKey)
            => $"{token}|{(int)type}|{resource}|{partitionKey}|{rowKey}";
    }
}
