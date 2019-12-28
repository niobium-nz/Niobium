namespace Cod
{
    public class PaymentInstruction
    {
        public PaymentInstruction(int amount, string reference, string instruction)
        {
            this.Amount = amount;
            this.Reference = reference;
            this.Instruction = instruction;
        }

        public int Amount { get; private set; }

        public string Reference { get; private set; }

        public string Instruction { get; private set; }
    }
}
