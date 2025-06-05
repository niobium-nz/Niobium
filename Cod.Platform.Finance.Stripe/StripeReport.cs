namespace Cod.Platform.Finance.Stripe
{
    public class StripeReport
    {
        public StripeReportKind Kind { get; set; }

        public required string Secret { get; set; }

        public required string ID { get; set; }
    }
}
