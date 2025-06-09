namespace Cod.Platform.Finance
{
    public class TransactionCreatedEvent
    {
        public TransactionCreatedEvent(Transaction newTransaction)
        {
            this.Transaction = newTransaction ?? throw new ArgumentNullException(nameof(newTransaction));
        }

        public Transaction Transaction { get; }

    }
}
