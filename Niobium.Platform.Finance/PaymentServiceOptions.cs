namespace Niobium.Platform.Finance
{
    public class PaymentServiceOptions
    {
        public string PaymentWebHookEndpoint { get; set; } = Constants.DefaultPaymentWebHookEndpoint;

        public string PaymentRequestEndpoint { get; set; } = Constants.DefaultPaymentRequestEndpoint;

        public required Dictionary<string, string> Secrets { get; set; }

        public required Dictionary<string, string> Hashes { get; set; }
    }
}
