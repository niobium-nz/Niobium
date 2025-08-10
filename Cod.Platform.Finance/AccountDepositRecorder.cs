using Cod.Finance;

namespace Cod.Platform.Finance
{
    public abstract class AccountDepositRecorder<TDomain, TEntity>(IDomainRepository<TDomain, TEntity> repo)
        : DomainEventHandler<IDomain<Transaction>, TransactionCreatedEvent>
        where TDomain : AccountableDomain<TEntity>
        where TEntity : class, new()
    {
        public override async Task HandleCoreAsync(TransactionCreatedEvent e, CancellationToken cancellationToken = default)
        {
            if (e.Transaction.ETag != null)
            {
                // If the transaction has an ETag, it means it has been processed before.
                return;
            }

            string pk = BuildPartitionKey(e.Transaction);
            string rk = BuildRowKey(e.Transaction);
            e.Transaction.PartitionKey = pk;
            e.Transaction.RowKey = rk;

            TDomain domain = await repo.GetAsync(pk, rk, cancellationToken: cancellationToken);
            await domain.MakeTransactionAsync(new[] { e.Transaction });
        }

        protected abstract string BuildPartitionKey(Transaction transaction);

        protected abstract string BuildRowKey(Transaction transaction);
    }
}
