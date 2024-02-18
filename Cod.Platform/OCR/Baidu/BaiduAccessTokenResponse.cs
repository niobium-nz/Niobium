namespace Cod.Platform.OCR.Baidu
{
    internal class BaiduAccessTokenResponse : BaiduIntegrationResponse
    {
        public string AccessToken { get; set; }

        public int ExpiresIn { get; set; }

        public TimeSpan GetExpiry()
        {
            return TimeSpan.FromDays((ExpiresIn / 60 / 60 / 24) - 1);
        }
    }
}
