namespace Cod.Platform
{
    public abstract class Constant
    {
        public const string STORAGE_CONNECTION_NAME = "STORAGE-ACCOUNT";
        public const string HTTP_PROXY_HOST = "PROXY-HOST";
        public const string HTTP_PROXY_USERNAME = "PROXY-USERNAME";
        public const string HTTP_PROXY_PASSWORD = "PROXY-PASSWORD";
        public const string AUTH_SECRET_NAME = "AUTH-TOKEN-KEY";
        public const string API_URL = "API_URL";

        public static readonly Currency CNY = Currency.Parse("CNY");
    }
}
