using Cod.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;

namespace Cod.Channel.Identity
{
    public class IdentityService(HttpClient httpClient, IOptions<IdentityServiceOptions> options, ILogger<IdentityService> logger)
    {
        public async Task<AccessTokenResponse> RequestIDTokenAsync(string scheme, string identity, string? credential, CancellationToken cancellationToken)
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
            var result = new AccessTokenResponse { StatusCode = response.StatusCode };
            if (!TryGetToken(response, out var token))
            {
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    return result;
                }

                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    var challenge = response.Headers.WwwAuthenticate.SingleOrDefault();
                    if (challenge != null)
                    {
                        _ = Enum.TryParse(challenge.Scheme, out AuthenticationKind challengeKind);
                        result.Challenge = challengeKind;
                        result.ChallengeSubject = challenge.Parameter;
                        return result;
                    }
                }

                return result;
            }
            else
            {
                result.Token = token;
                return result;
            }
        }

        public async Task<AccessTokenResponse> RefreshAccessTokenAsync(string idToken, CancellationToken cancellationToken)
        {
            var url = new Uri($"{options.Value.AccessTokenHost}{options.Value.AccessTokenEndpoint}");
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue(AuthenticationScheme.BearerLoginScheme, idToken);
            var response = await httpClient.SendAsync(request, cancellationToken);
            var result = new AccessTokenResponse { StatusCode = response.StatusCode };
            if (TryGetToken(response, out var token))
            {
                result.Token = token;
            }

            return result;
        }

        public async Task<ResourceTokenResponse> RequestResourceTokenAsync(string accessToken, ResourceType type, string resource, string? partition, string? id, CancellationToken cancellationToken)
        {
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
            request.Headers.Authorization = new AuthenticationHeaderValue(AuthenticationScheme.BearerLoginScheme, accessToken);
            var response = await httpClient.SendAsync(request, cancellationToken);
            var result = new ResourceTokenResponse { StatusCode = response.StatusCode };
            if (!response.IsSuccessStatusCode)
            {
                return result;
            }

            result.Token = await response.Content.ReadFromJsonAsync<StorageSignature>(cancellationToken);

            if (result.Token == null)
            {
                logger.LogWarning("Failed to parse StorageSignature from response.");
            }
            return result;
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

    }
}
