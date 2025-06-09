namespace Cod.Platform.Finance
{
    public class PaymentServiceOptions
    {
        public string PaymentWebHookEndpoint { get; set; } = Constants.DefaultPaymentWebHookEndpoint;

        public string PaymentRequestEndpoint { get; set; } = Constants.DefaultPaymentRequestEndpoint;

        public string SecretAPIKey { get; set; }

        public string SecretHashKey { get; set; }
    }
}
