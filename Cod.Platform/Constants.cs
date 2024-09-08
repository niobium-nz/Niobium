namespace Cod.Platform
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "CustomNaming")]
    public abstract class Constants : Cod.Constants
    {
        public const string ServiceEnvironment = "AZURE_FUNCTIONS_ENVIRONMENT";
        public const string ProductionEnvironment = "Production";
        public const string StagingEnvironment = "Staging";
        public const string DevelopmentEnvironment = "Development";

        public const string STORAGE_CONNECTION_NAME = "STORAGE_ACCOUNT";
        public const string HTTP_PROXY_HOST = "PROXY_HOST";
        public const string HTTP_PROXY_USERNAME = "PROXY_USERNAME";
        public const string HTTP_PROXY_PASSWORD = "PROXY_PASSWORD";
        public const string AccessTokenSecret = "AUTH_TOKEN_KEY";
        public const string API_URL = "API_URL";

        public const string NEW_STORAGE_CONNECTION_NAME = "STORAGE_ACCOUNT";
        public const string NEW_HTTP_PROXY_HOST = "PROXY_HOST";
        public const string NEW_HTTP_PROXY_USERNAME = "PROXY_USERNAME";
        public const string NEW_HTTP_PROXY_PASSWORD = "PROXY_PASSWORD";
        public const string NEW_AUTH_SECRET_NAME = "AUTH_TOKEN_KEY";

        public static readonly Currency CNY = Currency.Parse("CNY");
        public static readonly Currency USD = Currency.Parse("USD");
        public static readonly Currency AUD = Currency.Parse("AUD");
        public static readonly Currency NZD = Currency.Parse("NZD");
    }
}