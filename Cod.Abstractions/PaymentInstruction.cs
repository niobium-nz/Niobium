namespace Cod
{
    public class PaymentInstruction
    {
        /// <summary>
        /// 金额 单位为分
        /// </summary>
        public int Amount { get; set; }

        public string Reference { get; set; }

        public string Instruction { get; set; }

        public PaymentMethod Method { get; set; }

        public int Reason { get; set; }
    }
}
