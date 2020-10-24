using System;

namespace Cod.Platform
{
    public class ChargeResult
    {
        public ChargeTargetKind TargetKind { get; set; }

        public string Target { get; set; }

        public PaymentChannels Channel { get; set; }

        public PaymentKind PaymentKind { get; set; }

        public string Source { get; set; }

        public string Reference { get; set; }

        public object Account { get; set; }

        public int Amount { get; set; }

        public Currency Currency { get; set; }

        public string UpstreamID { get; set; }

        public DateTimeOffset AuthorizedAt { get; set; }

        public Transaction Transaction { get; set; }
    }
}
