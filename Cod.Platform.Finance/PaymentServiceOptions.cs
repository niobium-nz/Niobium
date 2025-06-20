namespace Cod.Platform.Finance
{
    public class PaymentServiceOptions
    {
        public string PaymentWebHookEndpoint { get; set; } = Constants.DefaultPaymentWebHookEndpoint;

        public string PaymentRequestEndpoint { get; set; } = Constants.DefaultPaymentRequestEndpoint;

        public required string SecretAPIKey { get; set; }

        public required string SecretHashKey { get; set; }
    }
}
