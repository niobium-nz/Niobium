namespace Cod
{
    public abstract class Entitlements
    {

        public const string BusinessScopePlaceholder = "{{BUSINESS_SCOPE}}";
        public const string CustomScopePlaceholder = "{{CUSTOM_SCOPE}}";

        public static readonly string[] ScopeSplitor = new[] { ":" };
        public static readonly string[] ValueSplitor = new[] { "," };
        public const string CategoryNamingPrefix = "COD-";

        public const string Account = CategoryNamingPrefix + "ACCOUNT";
        public const string AccountList = "ACCOUNT_LIST";
        public const string AccountManage = "ACCOUNT_MANAGE";
    }
}
