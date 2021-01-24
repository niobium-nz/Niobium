namespace Cod.Platform
{
    public abstract class Constant : Cod.Constant
    {
        public const string STORAGE_CONNECTION_NAME = "STORAGE-ACCOUNT";
        public const string HTTP_PROXY_HOST = "PROXY-HOST";
        public const string HTTP_PROXY_USERNAME = "PROXY-USERNAME";
        public const string HTTP_PROXY_PASSWORD = "PROXY-PASSWORD";
        public const string AUTH_SECRET_NAME = "AUTH-TOKEN-KEY";
        public const string API_URL = "API_URL";

        public const string NEW_STORAGE_CONNECTION_NAME = "STORAGE_ACCOUNT";
        public const string NEW_HTTP_PROXY_HOST = "PROXY_HOST";
        public const string NEW_HTTP_PROXY_USERNAME = "PROXY_USERNAME";
        public const string NEW_HTTP_PROXY_PASSWORD = "PROXY_PASSWORD";
        public const string NEW_AUTH_SECRET_NAME = "AUTH_TOKEN_KEY";

        public static readonly Currency CNY = Currency.Parse("CNY");
    }
}