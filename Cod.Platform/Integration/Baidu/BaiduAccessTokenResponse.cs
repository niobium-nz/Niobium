namespace Cod.Platform
{
    internal class BaiduAccessTokenResponse : BaiduIntegrationResponse
    {
        public string AccessToken { get; set; }

        public int ExpiresIn { get; set; }

        public TimeSpan GetExpiry() => TimeSpan.FromDays(this.ExpiresIn / 60 / 60 / 24 - 1);
    }
}
