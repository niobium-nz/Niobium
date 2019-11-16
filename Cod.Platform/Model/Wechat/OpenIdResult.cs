namespace Cod.Platform.Model.Wechat
{
    public class OpenIdResult
    {
        public string AccessToken { get; set; }

        public string ExpiresIn { get; set; }

        public string RefreshToken { get; set; }

        public string Openid { get; set; }

        public string Scope { get; set; }
    }
}
