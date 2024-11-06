using Cod.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.Net.Http.Headers;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;

namespace Cod.Channel.Identity
{
    public class DefaultAuthenticator(
        IOptions<IdentityServiceOptions> options,
        HttpClient httpClient,
        Lazy<IEnumerable<IDomainEventHandler<IAuthenticator>>> eventHandlers)
        : IAuthenticator, IAsyncDisposable
    {
        private readonly static Dictionary<string, StorageSignature> emptyResourceToken = [];
        private readonly SemaphoreSlim initializationLock = new(1, 1);
        private bool disposed;
        private bool isInitialized;
        private static string? savedIDToken;
        private static string? savedAccessToken;
        private readonly Dictionary<string, StorageSignature> signatures = emptyResourceToken;

        public JsonWebToken? IDToken { get; private set; }

        public JsonWebToken? AccessToken { get; private set; }

        public async Task<bool> GetAuthenticateStatus(CancellationToken cancellationToken)
        {
            await InitializeAsync();

            bool tokenRevoked = false;
            var now = DateTime.UtcNow;
            if (AccessToken != null)
            {
                if (now - AccessToken.ValidFrom > -options.Value.MaxClockSkewTolerence
                    && AccessToken.ValidTo - now > -options.Value.MaxClockSkewTolerence)
                {
                    return true;
                }

                await this.InitializeAccessTokenAsync(null, false);
                tokenRevoked = true;
            }

            if (IDToken != null 
                && now - IDToken.ValidFrom > -options.Value.MaxClockSkewTolerence
                && IDToken.ValidTo - now > -options.Value.MaxClockSkewTolerence)
            {
                await this.RefreshAccessTokenAsync(IDToken.EncodedToken, true, cancellationToken);
                now = DateTime.UtcNow;
                if (AccessToken != null
                    && now - AccessToken.ValidFrom > -options.Value.MaxClockSkewTolerence
                    && AccessToken.ValidTo - now > -options.Value.MaxClockSkewTolerence)
                {
                    return true;
                }
            }

            if (tokenRevoked)
            {
                await this.OnAuthenticationUpdated(false, cancellationToken);
            }
            return false;
        }

        public async Task<IEnumerable<Claim>?> GetClaimsAsync(CancellationToken cancellationToken)
        {
            await InitializeAsync();
            var authenticated = await this.GetAuthenticateStatus(cancellationToken);
            return authenticated ? this.AccessToken!.Claims : null;
        }

        public virtual async Task<LoginResult> LoginAsync(string scheme, string identity, string? credential, bool remember, CancellationToken cancellationToken)
        {
            await InitializeAsync();

            try
            {
                string authValue;
                if (scheme == AuthenticationScheme.BasicLoginScheme)
                {
                    var cred = credential == null ? string.Empty : credential.Trim();
                    var buffer = Encoding.ASCII.GetBytes($"{identity.Trim()}:{cred}");
                    authValue = Convert.ToBase64String(buffer);
                }
                else
                {
                    authValue = identity;
                }

                var url = new Uri($"{options.Value.IDTokenHost}{options.Value.IDTokenEndpoint}");
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new AuthenticationHeaderValue(scheme, authValue);

                var response = await httpClient.SendAsync(request, cancellationToken);
                if (!TryGetToken(response, out var token))
                {
                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        return new LoginResult { IsSuccess = false };
                    }

                    if (response.StatusCode == HttpStatusCode.Forbidden)
                    {
                        var challenge = response.Headers.WwwAuthenticate.SingleOrDefault();
                        if (challenge != null)
                        {
                            _ = Enum.TryParse(challenge.Scheme, out AuthenticationKind challengeKind);
                            return new LoginResult
                            {
                                IsSuccess = false,
                                Challenge = challengeKind,
                                ChallengeSubject = challenge.Parameter
                            };
                        }
                    }

                    await this.InitializeIDTokenAsync(null, false);
                    await this.OnAuthenticationUpdated(false, cancellationToken);
                    return new LoginResult { IsSuccess = false };
                }
                else
                {
                    await this.InitializeIDTokenAsync(token, remember);
                    await this.RefreshAccessTokenAsync(token, remember, cancellationToken);
                    var success = await this.GetAuthenticateStatus(cancellationToken);
                    await this.OnAuthenticationUpdated(true, cancellationToken);
                    return new LoginResult { IsSuccess = success };
                }
            }
            catch (Exception)
            {
                await LogoutAsync(cancellationToken);
                throw;
            }
        }

        public async Task LogoutAsync(CancellationToken cancellationToken)
        {
            await InitializeAsync();
            await this.InitializeIDTokenAsync(null, false);
            await this.OnAuthenticationUpdated(false, cancellationToken);
        }

        public virtual async Task<string?> RetrieveResourceTokenAsync(ResourceType type, string resource, string? partition, string? id, CancellationToken cancellationToken)
        {
            await InitializeAsync();
            var authenticated = await this.GetAuthenticateStatus(cancellationToken);
            if (!authenticated)
            {
                return null;
            }

            var key = BuildResourceTokenCacheKey(type, resource, partition, id);
            if (signatures.TryGetValue(key, out var value))
            {
                if (value.Expires < DateTimeOffset.UtcNow)
                {
                    this.signatures.Remove(key);
                    await this.SaveResourceTokensAsync(this.signatures);
                }
                else
                {
                    return value.Signature;
                }
            }

            var uri = new StringBuilder($"{options.Value.ResourceTokenHost}{options.Value.ResourceTokenEndpoint}?type={(int)type}&resource={resource.Trim()}");
            if (!String.IsNullOrWhiteSpace(partition))
            {
                uri.Append("&partition=");
                uri.Append(partition);
            }

            if (!String.IsNullOrWhiteSpace(id))
            {
                uri.Append("&id=");
                uri.Append(id);
            }

            var request = new HttpRequestMessage(HttpMethod.Get, new Uri(uri.ToString()));
            request.Headers.Authorization = new AuthenticationHeaderValue(AuthenticationScheme.BearerLoginScheme, AccessToken!.EncodedToken);
            var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    await this.InitializeIDTokenAsync(null, false);
                    await this.OnAuthenticationUpdated(false, cancellationToken);
                    return null;
                }
                else
                {
                    throw new ApplicationException((int)response.StatusCode);
                }
            }

            var result = await response.Content.ReadFromJsonAsync<StorageSignature>(cancellationToken);
            this.signatures.Add(key, result!);
            await this.SaveResourceTokensAsync(this.signatures);
            return result!.Signature;
        }

        protected virtual Task<string?> GetSavedIDTokenAsync()
        {
            return String.IsNullOrWhiteSpace(savedIDToken)
                ? Task.FromResult<string?>(null)
                : Task.FromResult<string?>(savedIDToken);
        }

        protected virtual Task SaveIDTokenAsync(string? token)
        {
            savedIDToken = token;
            return Task.CompletedTask;
        }

        protected virtual Task<string?> GetSavedAccessTokenAsync()
        {
            return String.IsNullOrWhiteSpace(savedAccessToken)
                ? Task.FromResult<string?>(null)
                : Task.FromResult<string?>(savedAccessToken);
        }

        protected virtual Task SaveAccessTokenAsync(string? token)
        {
            savedAccessToken = token;
            return Task.CompletedTask;
        }

        protected virtual Task<IDictionary<string, StorageSignature>> GetSavedResourceTokensAsync()
        {
            return Task.FromResult<IDictionary<string, StorageSignature>>(emptyResourceToken);
        }

        protected virtual Task SaveResourceTokensAsync(IDictionary<string, StorageSignature> resourceTokens)
            => Task.CompletedTask;

        protected virtual async Task InitializeAsync()
        {
            if (!isInitialized)
            {
                await initializationLock.WaitAsync();

                try
                {
                    if (!isInitialized)
                    {
                        var idt = await this.GetSavedIDTokenAsync();
                        if (!string.IsNullOrWhiteSpace(idt))
                        {
                            await this.InitializeIDTokenAsync(idt, true);
                        }

                        var act = await this.GetSavedAccessTokenAsync();
                        if (!string.IsNullOrWhiteSpace(act))
                        {
                            await this.InitializeAccessTokenAsync(act, true);
                        }

                        var ss = await this.GetSavedResourceTokensAsync();
                        if (ss.Count > 0)
                        {
                            await this.InitializeResourceTokensAsync(ss, true);
                        }

                        isInitialized = true;
                    }
                }
                finally
                {
                    initializationLock.Release();
                }
            }
        }

        protected async Task InitializeIDTokenAsync(string? token, bool remember)
        {
            IDToken = string.IsNullOrWhiteSpace(token) ? null : new JsonWebToken(token);
            var savedToken = await this.GetSavedIDTokenAsync();
            var isChanged = savedToken != token;

            if (isChanged)
            {
                await this.SaveIDTokenAsync(null);
                if (remember)
                {
                    await this.SaveIDTokenAsync(token);
                }

                await this.InitializeAccessTokenAsync(null, false);
            }
        }

        protected async Task InitializeAccessTokenAsync(string? token, bool remember)
        {
            AccessToken = string.IsNullOrWhiteSpace(token) ? null : new JsonWebToken(token);

            var savedToken = await this.GetSavedAccessTokenAsync();
            var isChanged = savedToken != token;
            if (isChanged)
            {
                if (remember)
                {
                    await this.SaveAccessTokenAsync(token);
                }
                else
                {
                    await this.SaveAccessTokenAsync(null);
                }

                await this.InitializeResourceTokensAsync(emptyResourceToken, false);
            }
        }

        protected async Task InitializeResourceTokensAsync(IDictionary<string, StorageSignature> tokens, bool remember)
        {
            this.signatures.Clear();
            foreach (var key in tokens.Keys)
            {
                this.signatures.Add(key, tokens[key]);
            }

            await this.SaveResourceTokensAsync(emptyResourceToken);
            if (remember)
            {
                await this.SaveResourceTokensAsync(this.signatures);
            }
        }

        protected virtual async Task RefreshAccessTokenAsync(string idToken, bool remember, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(idToken, nameof(idToken));
            var url = new Uri($"{options.Value.AccessTokenHost}{options.Value.AccessTokenEndpoint}");
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue(AuthenticationScheme.BearerLoginScheme, idToken);
            var response = await httpClient.SendAsync(request, cancellationToken);
            if (TryGetToken(response, out var token))
            {
                await this.InitializeAccessTokenAsync(token, remember);
            }
            else
            {
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    await this.InitializeIDTokenAsync(null, false);
                    await this.OnAuthenticationUpdated(false, cancellationToken);
                }
                else
                {
                    throw new ApplicationException((int)response.StatusCode);
                }
            }
        }

        protected async virtual Task OnAuthenticationUpdated(bool isAuthenticated, CancellationToken cancellationToken)
        {
            var e = new AuthenticationUpdatedEvent { IsAuthenticated = isAuthenticated };
            await eventHandlers.Value.InvokeAsync(e, cancellationToken);
        }

        private static bool TryGetToken(HttpResponseMessage response, [NotNullWhen(true)] out string? token)
        {
            if (response.IsSuccessStatusCode
                && response.Headers.TryGetValues(HeaderNames.Authorization, out var authHeaders)
                && !string.IsNullOrWhiteSpace(authHeaders?.SingleOrDefault())
                && AuthenticationHeaderValue.TryParse(authHeaders!.Single(), out var authHeader)
                && !string.IsNullOrWhiteSpace(authHeader.Parameter))
            {
                token = authHeader.Parameter;
                return true;
            }

            token = null;
            return false;
        }

        private static string BuildResourceTokenCacheKey(ResourceType type, string resource, string? partitionKey, string? rowKey)
            => $"{(int)type}|{resource}|{partitionKey ?? string.Empty}|{rowKey ?? string.Empty}";

        protected virtual ValueTask DisposeAsync(bool disposing)
        {
            if (disposing)
            {
                initializationLock.Dispose();
            }
            return ValueTask.CompletedTask;
        }

        public async ValueTask DisposeAsync()
        {
            if (!disposed)
            {
                await DisposeAsync(true);
                disposed = true;
            }
            GC.SuppressFinalize(this);
        }
    }
}
