using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cod.Platform
{
    public class BusinessDomain : PlatformDomain<Business>, IAccountable
    {
        public const int CompensationTransactionReason = 11;
        private readonly Lazy<IRepository<Interest>> interestRepository;
        private readonly Lazy<ICacheStore> cache;

        public BusinessDomain(
            Lazy<IRepository<Business>> repository,
            Lazy<IRepository<Interest>> interestRepository,
            Lazy<ICacheStore> cache)
            : base(repository)
        {
            this.interestRepository = interestRepository;
            this.cache = cache;
        }

        public ICacheStore CacheStore => this.cache.Value;

        public Task<string> GetAccountingPrincipalAsync() => Task.FromResult(this.RowKey);

        public async Task MakeCompensationAsync(DateTimeOffset fromInclusive, DateTimeOffset toInclusive)
        {
            var interests = await this.interestRepository.Value.GetAsync(Interest.BuildPartitionKey(Guid.Parse(this.RowKey)));
            var validInterests = interests.Where(i => i.Agreement > 0);
            var groups = validInterests.GroupBy(t => t.Target);
            foreach (var group in groups)
            {
                if (group.Count() > 1)
                {
                    throw new NotSupportedException();
                }
            }

            var transactions = await this.GetTransactionsAsync(fromInclusive, toInclusive);
            foreach (var interest in validInterests)
            {
                var relatedTransactions = transactions.Where(t => t.Reference == interest.Target);
                var income = this.FigureIncome(relatedTransactions);
                if (income < interest.Agreement)
                {
                    var compensation = interest.Agreement - income;

                    if (!interest.Percentage)
                    {
                        throw new NotImplementedException();
                    }

                    var costOnCompensation = (int)Math.Floor(compensation * (1 - (interest.Value / 10000m)));
                    if (costOnCompensation < 0)
                    {
                        throw new NotSupportedException();
                    }

                    var remark = await this.MakeCompensationTransactionRemarkAsync(interest.Target);
                    var id = relatedTransactions.Max(t => t.GetCreated());
                    await this.MakeTransactionAsync(compensation, CompensationTransactionReason, remark, interest.Target, id: id.AddMilliseconds(1).ToReverseUnixTimestamp());
                    await this.MakeTransactionAsync(-costOnCompensation, CompensationTransactionReason, remark, interest.Target, id: id.AddMilliseconds(2).ToReverseUnixTimestamp());
                }
            }
        }

        protected virtual int FigureIncome(IEnumerable<Transaction> input)
            => input.Where(t => t.Delta > 0).Select(t => t.Delta).DefaultIfEmpty().Sum(t => (int)(t * 100));

        protected virtual Task<string> MakeCompensationTransactionRemarkAsync(string reference)
            => Task.FromResult(reference);
    }
}
