namespace Cod.Platform
{
    public interface IAccountable
    {
        public string AccountingPrincipal { get; }

        IAsyncEnumerable<Transaction> GetTransactionsAsync(DateTimeOffset fromInclusive, DateTimeOffset toInclusive);

        Task<TransactionRequest> BuildTransactionAsync(double delta, int reason, string remark, string reference, string id = null, string corelation = null);

        Task<IEnumerable<Transaction>> MakeTransactionAsync(double delta, int reason, string remark, string reference, string id = null, string corelation = null);

        Task<IEnumerable<Transaction>> MakeTransactionAsync(TransactionRequest request);

        Task<IEnumerable<Transaction>> MakeTransactionAsync(IEnumerable<TransactionRequest> requests);

        Task<Transaction> GetTransactionAsync(DateTimeOffset id);

        Task<AccountBalance> GetBalanceAsync(DateTimeOffset input);
    }
}
