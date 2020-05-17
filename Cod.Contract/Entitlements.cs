namespace Cod
{
    public abstract class Entitlements
    {

        public static readonly string[] ScopeSplitor = new[] { ":" };
        public static readonly string[] ValueSplitor = new[] { "," };
        public static string CategoryNamingPrefix { get; set; }

        public const string Account = "ACCOUNT";
        public const string AccountManagement = "ACCOUNT_MANAGEMENT";

        public static readonly string NEWAccount = $"{CategoryNamingPrefix}/account";
        public const string NEWAccountList = "ACCOUNT_LIST";
        public const string NEWAccountManage = "ACCOUNT_MANAGE";
    }
}
