namespace Niobium
{
    public enum TransactionReason : int
    {
        Invalid = -1,

        Deposit = 4,

        Spend = 12,

        Adjustment = 11,

        Refund = 99,
    }
}
