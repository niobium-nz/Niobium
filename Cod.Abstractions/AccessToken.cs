namespace Cod
{
    public class AccessToken
    {
        public string Token { get; set; }

        public long Expiry { get; set; }

        public DateTimeOffset Expires => DateTimeOffset.FromUnixTimeSeconds(Expiry);

        public bool Validate()
        {
            return !string.IsNullOrWhiteSpace(Token) && Expires > DateTimeOffset.UtcNow;
        }
    }
}
