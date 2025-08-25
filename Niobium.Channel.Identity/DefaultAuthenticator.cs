using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Niobium.Identity;
using System.Net;
using System.Security.Claims;
using System.Text;

namespace Niobium.Channel.Identity
{
    public class DefaultAuthenticator(
        IOptions<IdentityServiceOptions> options,
        IdentityService identityService,
        Lazy<IEnumerable<IDomainEventHandler<IAuthenticator>>> eventHandlers)
        : IAuthenticator, IAsyncDisposable
    {
        private static readonly Dictionary<string, StorageSignature> emptyResourceToken = [];
        private readonly SemaphoreSlim initializationLock = new(1, 1);
        private bool disposed;
        private bool isInitialized;
        private static string? savedIDToken;
        private static string? savedAccessToken;
        private readonly Dictionary<string, StorageSignature> signatures = emptyResourceToken;

        public JsonWebToken? IDToken { get; private set; }

        public JsonWebToken? AccessToken { get; private set; }

        public async Task<bool> GetAuthenticateStatus(CancellationToken cancellationToken = default)
        {
            await InitializeAsync();

            bool tokenRevoked = false;
            DateTime now = DateTime.UtcNow;
            if (AccessToken != null)
            {
                if (now - AccessToken.ValidFrom > -options.Value.MaxClockSkewTolerence
                    && AccessToken.ValidTo - now > -options.Value.MaxClockSkewTolerence)
                {
                    return true;
                }

                await InitializeAccessTokenAsync(null, false);
                tokenRevoked = true;
            }

            if (IDToken != null
                && now - IDToken.ValidFrom > -options.Value.MaxClockSkewTolerence
                && IDToken.ValidTo - now > -options.Value.MaxClockSkewTolerence)
            {
                await RefreshAccessTokenAsync(IDToken.EncodedToken, true, cancellationToken);
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
                await OnAuthenticationUpdated(false, cancellationToken);
            }
            return false;
        }

        public async Task<IEnumerable<Claim>?> GetClaimsAsync(CancellationToken cancellationToken = default)
        {
            await InitializeAsync();
            bool authenticated = await GetAuthenticateStatus(cancellationToken);
            return authenticated ? AccessToken!.Claims : null;
        }

        public virtual async Task<LoginResult> LoginAsync(string scheme, string identity, string? credential, bool remember, CancellationToken cancellationToken = default)
        {
            await InitializeAsync();

            try
            {
                AccessTokenResponse result = await identityService.RequestIDTokenAsync(scheme, identity, credential, cancellationToken);
                if (result.StatusCode == HttpStatusCode.Unauthorized)
                {
                    return new LoginResult { IsSuccess = false };
                }

                if (result.StatusCode == HttpStatusCode.Forbidden && result.Challenge.HasValue)
                {
                    return new LoginResult
                    {
                        IsSuccess = false,
                        Challenge = result.Challenge.Value,
                        ChallengeSubject = result.ChallengeSubject
                    };
                }

                if (result.Token == null)
                {
                    await InitializeIDTokenAsync(null, false);
                    await OnAuthenticationUpdated(false, cancellationToken);
                    return new LoginResult { IsSuccess = false };
                }
                else
                {
                    await InitializeIDTokenAsync(result.Token, remember);
                    await RefreshAccessTokenAsync(result.Token, remember, cancellationToken);
                    bool success = await GetAuthenticateStatus(cancellationToken);
                    await OnAuthenticationUpdated(true, cancellationToken);
                    return new LoginResult { IsSuccess = success };
                }
            }
            catch (Exception)
            {
                await LogoutAsync(cancellationToken);
                throw;
            }
        }

        public async Task LogoutAsync(CancellationToken cancellationToken = default)
        {
            await InitializeAsync();
            await InitializeIDTokenAsync(null, false);
            await OnAuthenticationUpdated(false, cancellationToken);
        }

        public virtual async Task<string> RetrieveResourceTokenAsync(ResourceType type, string resource, string? partition, string? id, CancellationToken cancellationToken = default)
        {
            await InitializeAsync();
            bool authenticated = await GetAuthenticateStatus(cancellationToken);
            if (!authenticated)
            {
                throw new ApplicationException(InternalError.AuthenticationRequired);
            }

            string key = BuildResourceTokenCacheKey(type, resource, partition, id);
            if (signatures.TryGetValue(key, out StorageSignature? value))
            {
                if (value.Expires < DateTimeOffset.UtcNow)
                {
                    signatures.Remove(key);
                    await SaveResourceTokensAsync(signatures);
                }
                else
                {
                    return value.Signature;
                }
            }

            StringBuilder uri = new($"{options.Value.ResourceTokenHost}{options.Value.ResourceTokenEndpoint}?type={(int)type}&resource={resource.Trim()}");
            if (!string.IsNullOrWhiteSpace(partition))
            {
                uri.Append("&partition=");
                uri.Append(partition);
            }

            if (!string.IsNullOrWhiteSpace(id))
            {
                uri.Append("&id=");
                uri.Append(id);
            }

            ResourceTokenResponse result = await identityService.RequestResourceTokenAsync(AccessToken!.EncodedToken, type, resource, partition, id, cancellationToken);
            if (result.StatusCode == HttpStatusCode.Unauthorized || result.Token == null)
            {
                await InitializeIDTokenAsync(null, false);
                await OnAuthenticationUpdated(false, cancellationToken);
                throw new ApplicationException((int)result.StatusCode, "Resource access authorization failed");
            }

            signatures.Add(key, result.Token);
            await SaveResourceTokensAsync(signatures);
            return result.Token.Signature;
        }

        protected virtual Task<string?> GetSavedIDTokenAsync()
        {
            return string.IsNullOrWhiteSpace(savedIDToken)
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
            return string.IsNullOrWhiteSpace(savedAccessToken)
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
        {
            return Task.CompletedTask;
        }

        protected virtual async Task InitializeAsync()
        {
            if (!isInitialized)
            {
                await initializationLock.WaitAsync();

                try
                {
                    if (!isInitialized)
                    {
                        string? idt = await GetSavedIDTokenAsync();
                        if (!string.IsNullOrWhiteSpace(idt))
                        {
                            await InitializeIDTokenAsync(idt, true);
                        }

                        string? act = await GetSavedAccessTokenAsync();
                        if (!string.IsNullOrWhiteSpace(act))
                        {
                            await InitializeAccessTokenAsync(act, true);
                        }

                        IDictionary<string, StorageSignature> ss = await GetSavedResourceTokensAsync();
                        if (ss.Count > 0)
                        {
                            await InitializeResourceTokensAsync(ss, true);
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
            string? savedToken = await GetSavedIDTokenAsync();
            bool isChanged = savedToken != token;

            if (isChanged)
            {
                await SaveIDTokenAsync(null);
                if (remember)
                {
                    await SaveIDTokenAsync(token);
                }

                await InitializeAccessTokenAsync(null, false);
            }
        }

        protected async Task InitializeAccessTokenAsync(string? token, bool remember)
        {
            AccessToken = string.IsNullOrWhiteSpace(token) ? null : new JsonWebToken(token);

            string? savedToken = await GetSavedAccessTokenAsync();
            bool isChanged = savedToken != token;
            if (isChanged)
            {
                if (remember)
                {
                    await SaveAccessTokenAsync(token);
                }
                else
                {
                    await SaveAccessTokenAsync(null);
                }

                await InitializeResourceTokensAsync(emptyResourceToken, false);
            }
        }

        protected async Task InitializeResourceTokensAsync(IDictionary<string, StorageSignature> tokens, bool remember)
        {
            signatures.Clear();
            foreach (string key in tokens.Keys)
            {
                signatures.Add(key, tokens[key]);
            }

            await SaveResourceTokensAsync(emptyResourceToken);
            if (remember)
            {
                await SaveResourceTokensAsync(signatures);
            }
        }

        protected virtual async Task RefreshAccessTokenAsync(string idToken, bool remember, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(idToken, nameof(idToken));
            AccessTokenResponse result = await identityService.RefreshAccessTokenAsync(idToken, cancellationToken);
            if (result.Token != null)
            {
                await InitializeAccessTokenAsync(result.Token, remember);
            }
            else
            {
                if (result.StatusCode == HttpStatusCode.Unauthorized)
                {
                    await InitializeIDTokenAsync(null, false);
                    await OnAuthenticationUpdated(false, cancellationToken);
                }
                else
                {
                    throw new ApplicationException((int)result.StatusCode, "Failed to refresh access token.");
                }
            }
        }

        protected virtual async Task OnAuthenticationUpdated(bool isAuthenticated, CancellationToken cancellationToken)
        {
            AuthenticationUpdatedEvent e = new() { IsAuthenticated = isAuthenticated };
            await eventHandlers.Value.InvokeAsync(e, cancellationToken);
        }

        private static string BuildResourceTokenCacheKey(ResourceType type, string resource, string? partitionKey, string? rowKey)
        {
            return $"{(int)type}|{resource}|{partitionKey ?? string.Empty}|{rowKey ?? string.Empty}";
        }

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
