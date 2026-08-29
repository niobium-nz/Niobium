namespace Niobium
{
    public abstract class Constants
    {
        public const string AspNetCoreEnvironment = "ASPNETCORE_ENVIRONMENT";
        public const string ServiceEnvironment = "AZURE_FUNCTIONS_ENVIRONMENT";
        public const string ProdEnvironment = "Prod";
        public const string ProductionEnvironment = "Production";
        public const string StagingEnvironment = "Staging";
        public const string TestEnvironment = "Test";
        public const string DevelopmentEnvironment = "Development";
        public const string DevEnvironment = "Dev";
        public const string LocalEnvironment = "Local";

        public const string TransactionReasonDeposit = "TransactionReason_Deposit";
        public const string TransactionReasonRefund = "TransactionReason_Refund";
        public const string CustomClaimPrefix = "NB-";
    }
}