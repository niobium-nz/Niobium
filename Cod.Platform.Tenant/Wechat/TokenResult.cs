namespace Cod.Platform.Tenant.Wechat
{
    public class TokenResult
    {
        public string AccessToken { get; set; }

        public int ExpiresIn { get; set; }

        public string Errmsg { get; set; }

        public int Errcode { get; set; }
    }
}
