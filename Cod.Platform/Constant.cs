namespace Cod.Platform
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "CustomNaming")]
    public abstract class Constant : Cod.Constant
    {
        public const string BLOB_ENDPOINT = "BLOB_ENDPOINT";
        public const string QUEUE_ENDPOINT = "QUEUE_ENDPOINT";
        public const string TABLE_ENDPOINT = "TABLE_ENDPOINT";

        public const string STORAGE_CONNECTION_NAME = "STORAGE_ACCOUNT";
        public const string HTTP_PROXY_HOST = "PROXY_HOST";
        public const string HTTP_PROXY_USERNAME = "PROXY_USERNAME";
        public const string HTTP_PROXY_PASSWORD = "PROXY_PASSWORD";
        public const string AUTH_SECRET_NAME = "AUTH_TOKEN_KEY";
        public const string API_URL = "API_URL";

        public const string NEW_STORAGE_CONNECTION_NAME = "STORAGE_ACCOUNT";
        public const string NEW_HTTP_PROXY_HOST = "PROXY_HOST";
        public const string NEW_HTTP_PROXY_USERNAME = "PROXY_USERNAME";
        public const string NEW_HTTP_PROXY_PASSWORD = "PROXY_PASSWORD";
        public const string NEW_AUTH_SECRET_NAME = "AUTH_TOKEN_KEY";

        public static readonly Currency CNY = Currency.Parse("CNY");
    }
}