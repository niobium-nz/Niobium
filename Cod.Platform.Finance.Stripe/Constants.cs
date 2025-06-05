namespace Cod.Platform.Finance.Stripe
{
    public abstract class Constants : Cod.Platform.Finance.Constants
    {
        public const string DefaultPaymentWebHookEndpoint = "payments/webhook";
        public const string DefaultPaymentRequestEndpoint = "payments/init";

        public const string MetadataTargetKindKey = "kind";
        public const string MetadataTargetKey = "target";
        public const string MetadataOrderKey = "order";
        public const string MetadataReferenceKey = "reference";
        public const string MetadataIntentUserID = "user";
    }
}
