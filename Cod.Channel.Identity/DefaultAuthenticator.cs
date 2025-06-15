using Cod.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Net;
using System.Security.Claims;
using System.Text;

namespace Cod.Channel.Identity
{
    public class DefaultAuthenticator(
        IOptions<IdentityServiceOptions> options,
        IdentityService identityService,
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

        public async Task<bool> GetAuthenticateStatus(CancellationToken cancellationToken = default)
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

        public async Task<IEnumerable<Claim>?> GetClaimsAsync(CancellationToken cancellationToken = default)
        {
            await InitializeAsync();
            var authenticated = await this.GetAuthenticateStatus(cancellationToken);
            return authenticated ? this.AccessToken!.Claims : null;
        }

        public virtual async Task<LoginResult> LoginAsync(string scheme, string identity, string? credential, bool remember, CancellationToken cancellationToken = default)
        {
            await InitializeAsync();

            try
            {
                var result = await identityService.RequestIDTokenAsync(scheme, identity, credential, cancellationToken);
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
                    await this.InitializeIDTokenAsync(null, false);
                    await this.OnAuthenticationUpdated(false, cancellationToken);
                    return new LoginResult { IsSuccess = false };
                }
                else
                {
                    await this.InitializeIDTokenAsync(result.Token, remember);
                    await this.RefreshAccessTokenAsync(result.Token, remember, cancellationToken);
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

        public async Task LogoutAsync(CancellationToken cancellationToken = default)
        {
            await InitializeAsync();
            await this.InitializeIDTokenAsync(null, false);
            await this.OnAuthenticationUpdated(false, cancellationToken);
        }

        public virtual async Task<string> RetrieveResourceTokenAsync(ResourceType type, string resource, string? partition, string? id, CancellationToken cancellationToken = default)
        {
            await InitializeAsync();
            var authenticated = await this.GetAuthenticateStatus(cancellationToken);
            if (!authenticated)
            {
                throw new ApplicationException(InternalError.AuthenticationRequired);
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

            var result = await identityService.RequestResourceTokenAsync(AccessToken!.EncodedToken, type, resource, partition, id, cancellationToken);
            if (result.StatusCode == HttpStatusCode.Unauthorized || result.Token == null)
            {
                await this.InitializeIDTokenAsync(null, false);
                await this.OnAuthenticationUpdated(false, cancellationToken);
                throw new ApplicationException((int)result.StatusCode, "Resource access authorization failed");
            }

            this.signatures.Add(key, result.Token);
            await this.SaveResourceTokensAsync(this.signatures);
            return result.Token.Signature;
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
            var result = await identityService.RefreshAccessTokenAsync(idToken, cancellationToken);
            if (result.Token != null)
            {
                await this.InitializeAccessTokenAsync(result.Token, remember);
            }
            else
            {
                if (result.StatusCode == HttpStatusCode.Unauthorized)
                {
                    await this.InitializeIDTokenAsync(null, false);
                    await this.OnAuthenticationUpdated(false, cancellationToken);
                }
                else
                {
                    throw new ApplicationException((int)result.StatusCode, "Failed to refresh access token.");
                }
            }
        }

        protected async virtual Task OnAuthenticationUpdated(bool isAuthenticated, CancellationToken cancellationToken)
        {
            var e = new AuthenticationUpdatedEvent { IsAuthenticated = isAuthenticated };
            await eventHandlers.Value.InvokeAsync(e, cancellationToken);
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
