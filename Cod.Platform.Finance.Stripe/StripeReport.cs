namespace Cod.Platform.Finance.Stripe
{
    internal sealed class StripeReport
    {
        public const string TypeCharge = "charge.succeeded";
        public const string TypeSetup = "setup_intent.succeeded";
        public const string TypeRefund = "refund.succeeded";

        public static readonly string[] SupportedTypes = [TypeCharge, TypeSetup, TypeRefund];

        /// <summary>
        /// Event Type
        /// </summary>
        public required string Type { get; set; }

        /// <summary>
        /// Stripe Event ID
        /// </summary>
        public required string ID { get; set; }

        public required StripeReportData Data { get; set; }

        public bool Livemode { get; set; }
    }

    internal sealed class StripeReportData
    {
        /// <summary>
        /// The object type, e.g. "charge", "setup_intent", "refund"
        /// </summary>
        public required StripeReportDataObject Object { get; set; }
    }

    internal sealed class StripeReportDataObject
    {
        /// <summary>
        /// The unique identifier for the object, e.g. charge ID, setup intent ID, refund ID
        /// </summary>
        public required string ID { get; set; }

        /// <summary>
        /// The object type, e.g. "charge", "setup_intent", "refund"
        /// </summary>
        public required string Object { get; set; }
    }

}
