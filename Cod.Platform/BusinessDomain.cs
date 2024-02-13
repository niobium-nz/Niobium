using Cod.Platform.Entity;
using Microsoft.Extensions.Logging;

namespace Cod.Platform
{
    public class BusinessDomain : AccountableDomain<Business>
    {
        public const int CompensationTransactionReason = 11;
        private readonly Lazy<IRepository<Interest>> interestRepository;

        public BusinessDomain(
            Lazy<IRepository<Business>> repository,
            Lazy<IRepository<Interest>> interestRepository,
            Lazy<IQueryableRepository<Transaction>> transactionRepo,
            Lazy<IQueryableRepository<Accounting>> accountingRepo,
            Lazy<IEnumerable<IAccountingAuditor>> auditors,
            Lazy<ICacheStore> cache,
            ILogger logger)
            : base(repository, transactionRepo, accountingRepo, auditors, cache, logger)
        {
            this.interestRepository = interestRepository;
        }

        public override string AccountingPrincipal => RowKey;

        public async Task MakeCompensationAsync(DateTimeOffset fromInclusive, DateTimeOffset toInclusive)
        {
            var interests = await interestRepository.Value.GetAsync(Interest.BuildPartitionKey(Guid.Parse(RowKey))).ToListAsync();
            IEnumerable<Interest> validInterests = interests.Where(i => i.Agreement > 0);
            IEnumerable<IGrouping<string, Interest>> groups = validInterests.GroupBy(t => t.Target);
            foreach (IGrouping<string, Interest> group in groups)
            {
                if (group.Count() > 1)
                {
                    throw new NotSupportedException();
                }
            }

            var transactions = await GetTransactionsAsync(fromInclusive, toInclusive).ToListAsync();
            foreach (Interest interest in validInterests)
            {
                IEnumerable<Transaction> relatedTransactions = transactions.Where(t => t.Reference == interest.Target);
                int income = FigureIncome(relatedTransactions);
                if (income < interest.Agreement)
                {
                    int compensation = interest.Agreement - income;

                    if (!interest.Percentage)
                    {
                        throw new NotImplementedException();
                    }

                    int costOnCompensation = (int)Math.Floor(compensation * (1 - (interest.Value / 10000m)));
                    if (costOnCompensation < 0)
                    {
                        throw new NotSupportedException();
                    }

                    string remark = await MakeCompensationTransactionRemarkAsync(interest.Target);
                    DateTimeOffset id = relatedTransactions.Max(t => t.GetCreated());
                    await MakeTransactionAsync(compensation / 100d, CompensationTransactionReason, remark, interest.Target, id: id.AddMilliseconds(1).ToReverseUnixTimestamp());
                    await MakeTransactionAsync(-costOnCompensation / 100d, CompensationTransactionReason, remark, interest.Target, id: id.AddMilliseconds(2).ToReverseUnixTimestamp());
                }
            }
        }

        protected virtual int FigureIncome(IEnumerable<Transaction> input)
        {
            return input.Where(t => t.Delta > 0).Select(t => t.Delta).DefaultIfEmpty().Sum(t => (int)(t * 100));
        }

        protected virtual Task<string> MakeCompensationTransactionRemarkAsync(string reference)
        {
            return Task.FromResult(reference);
        }
    }
}
