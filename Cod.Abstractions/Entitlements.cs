namespace Cod
{
    public abstract class Entitlements
    {

        public const string BusinessScopePlaceholder = "{{BUSINESS_SCOPE}}";
        public const string CustomScopePlaceholder = "{{CUSTOM_SCOPE}}";

        public static readonly string[] ScopeSplitor = new[] { ":" };
        public static readonly string[] ValueSplitor = new[] { "," };
        public const string CategoryNamingPrefix = "COD-";

        public const string NEWAccount = CategoryNamingPrefix + "ACCOUNT";
        public const string NEWAccountList = "ACCOUNT_LIST";
        public const string NEWAccountManage = "ACCOUNT_MANAGE";
    }
}
