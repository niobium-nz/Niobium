using Niobium.Identity;
using System.Net;

namespace Niobium.Channel.Identity
{
    public class AccessTokenResponse
    {
        public string? Token { get; set; }

        public AuthenticationKind? Challenge { get; set; }

        public string? ChallengeSubject { get; set; }

        public HttpStatusCode StatusCode { get; set; }
    }
}
