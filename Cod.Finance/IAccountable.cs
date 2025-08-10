namespace Cod.Finance
{
    public interface IAccountable
    {
        string AccountingPrincipal { get; }

        IAsyncEnumerable<Transaction> GetTransactionsAsync(DateTimeOffset fromInclusive, DateTimeOffset toInclusive);

        Task<TransactionRequest> BuildTransactionAsync(long delta, int reason, string remark, string reference, string? id = null, string? corelation = null);

        Task<IEnumerable<Transaction>> MakeTransactionAsync(long delta, int reason, string remark, string reference, string? id = null, string? corelation = null);

        Task<IEnumerable<Transaction>> MakeTransactionAsync(TransactionRequest request);

        Task<IEnumerable<Transaction>> MakeTransactionAsync(IEnumerable<TransactionRequest> requests);

        Task<Transaction?> GetTransactionAsync(DateTimeOffset id);

        Task<AccountBalance> GetBalanceAsync(DateTimeOffset input, CancellationToken cancellationToken = default);
    }
}
