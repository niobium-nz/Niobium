using Cod.Identity;
using System.Net;

namespace Cod.Channel.Identity
{
    public class AccessTokenResponse
    {
        public string? Token { get; set; }

        public AuthenticationKind? Challenge { get; set; }

        public string? ChallengeSubject { get; set; }

        public HttpStatusCode StatusCode { get; set; }
    }
}
