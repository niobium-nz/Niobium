namespace Niobium.Finance
{
    public class ChargeRequest
    {
        public required string Tenant { get; set; }

        public ChargeTargetKind TargetKind { get; set; }

        public required string Target { get; set; }

        public PaymentChannels Channel { get; set; }

        public PaymentOperationKind Operation { get; set; }

        public string? Order { get; set; }

        public string? Reference { get; set; }

        public object? Account { get; set; }

        public long Amount { get; set; }

        public Currency Currency { get; set; }

        public string? Description { get; set; }

        public string? ReturnUri { get; set; }

        public string? IP { get; set; }
    }
}
