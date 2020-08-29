using System;

namespace Cod
{
    public class AccessToken
    {
        public string Token { get; set; }

        public long Expiry { get; set; }

        public DateTimeOffset Expires => DateTimeOffset.FromUnixTimeSeconds(this.Expiry);

        public bool Validate() => !String.IsNullOrWhiteSpace(this.Token) && this.Expires > DateTimeOffset.UtcNow;
    }
}
