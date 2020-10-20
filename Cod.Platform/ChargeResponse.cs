namespace Cod.Platform
{
    public class ChargeResponse
    {
        public int Amount { get; set; }

        public string Reference { get; set; }

        public string Extra { get; set; }

        public PaymentMethodKind Method { get; set; }

        public int Reason { get; set; }

        public string UpstreamID { get; set; }

        public object Instruction { get; set; }
    }
}
