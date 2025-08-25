namespace Niobium.Finance
{
    public class TransactionRequest
    {
        public string? ID { get; set; }

        public required string Target { get; set; }

        public long Delta { get; set; }

        public int Reason { get; set; }

        public string? Remark { get; set; }

        public string? Reference { get; set; }

        public string? Corelation { get; set; }
    }
}