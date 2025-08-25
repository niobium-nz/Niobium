namespace Niobium.Finance
{
    public class ChargeResult
    {
        public ChargeTargetKind TargetKind { get; set; }

        public required string Target { get; set; }

        public PaymentChannels Channel { get; set; }

        public PaymentOperationKind PaymentKind { get; set; }

        public string? Source { get; set; }

        public string? Reference { get; set; }

        public object? Account { get; set; }

        public long Amount { get; set; }

        public Currency Currency { get; set; }

        public required string UpstreamID { get; set; }

        public DateTimeOffset AuthorizedAt { get; set; }

        public Transaction? Transaction { get; set; }
    }
}
