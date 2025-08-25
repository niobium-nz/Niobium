namespace Niobium
{
    public abstract class Entitlements
    {

        public const string BusinessScopePlaceholder = "{{BUSINESS_SCOPE}}";
        public const string CustomScopePlaceholder = "{{CUSTOM_SCOPE}}";

        public static readonly char[] ScopeSplitor = [':'];
        public static readonly char[] ValueSplitor = [','];
        public const string CategoryNamingPrefix = "COD-";

        public const string Account = CategoryNamingPrefix + "ACCOUNT";
        public const string AccountList = "ACCOUNT_LIST";
        public const string AccountManage = "ACCOUNT_MANAGE";
    }
}
