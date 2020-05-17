namespace Cod
{
    public abstract class Entitlements
    {

        public static readonly string[] ScopeSplitor = new[] { ":" };
        public static readonly string[] ValueSplitor = new[] { "," };
        public const string CategoryNamingPrefix = "COD-";

        public const string Account = "ACCOUNT";
        public const string AccountManagement = "ACCOUNT_MANAGEMENT";

        public const string NEWAccount = CategoryNamingPrefix + "ACCOUNT";
        public const string NEWAccountList = "ACCOUNT_LIST";
        public const string NEWAccountManage = "ACCOUNT_MANAGE";
    }
}
