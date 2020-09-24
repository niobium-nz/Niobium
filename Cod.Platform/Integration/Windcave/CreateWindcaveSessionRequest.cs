namespace Cod.Platform
{
    internal class CreateWindcaveSessionRequest
    {
        public CreateWindcaveSessionRequest(CreditCardTransactionKind kind, Currency currency, int amount)
        {
            this.Type = kind.ToWindcaveType();
            this.Amount = (amount / 100d).ToString();
            this.Currency = currency.ToString();
            this.CallbackUrls = new WindcaveRedirectionDefinition();
        }

        public string Type { get; set; }

        public string Amount { get; set; }

        public string Currency { get; set; }

        public int StoreCard { get; } = 1;

        public string StoreCardIndicator { get; } = "credentialonfileinitial";

        public string MerchantReference { get; set; }

        public string NotificationUrl { get; set; }

        public WindcaveRedirectionDefinition CallbackUrls { get; set; }
    }
}
