namespace Cod.Platform.Finance.Stripe
{
    public class PaymentServiceOptions
    {
        public required string PaymentWebHookEndpoint { get; set; } = Constants.DefaultPaymentWebHookEndpoint;

        public required string PaymentRequestEndpoint { get; set; } = Constants.DefaultPaymentRequestEndpoint;

        public required string SecretAPIKey { get; set; }
    }
}
