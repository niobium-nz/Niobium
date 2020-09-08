using System;

namespace Cod.Platform
{
    public class TransactionRequest
    {
        public string ID { get; set; }

        public string Target { get; set; }

        public double Delta { get; set; }

        public int Reason { get; set; }

        public string Remark { get; set; }

        public string Reference { get; set; }

        public string Corelation { get; set; }

        public TransactionRequest(string target, double delta)
        {
            this.Target = target;
            this.Delta = delta;
        }
    }
}