using Cod.Platform.Entity;
using Microsoft.Extensions.Logging;

namespace Cod.Platform
{
    public abstract class AccountableDomain<T> : PlatformDomain<T>, IAccountable
        where T : IEntity
    {
        private const string FrozenKey = "frozen";
        private const string DeltaKey = "delta";
        private readonly Lazy<IQueryableRepository<Transaction>> transactionRepo;
        private readonly Lazy<IQueryableRepository<Accounting>> accountingRepo;
        private readonly Lazy<IEnumerable<IAccountingAuditor>> auditors;
        private readonly Lazy<ICacheStore> cacheStore;
        private readonly ILogger logger;

        public AccountableDomain(
            Lazy<IRepository<T>> repo,
            Lazy<IQueryableRepository<Transaction>> transactionRepo,
            Lazy<IQueryableRepository<Accounting>> accountingRepo,
            Lazy<IEnumerable<IAccountingAuditor>> auditors,
            Lazy<ICacheStore> cacheStore,
            ILogger logger)
            : base(repo)
        {
            this.transactionRepo = transactionRepo;
            this.accountingRepo = accountingRepo;
            this.auditors = auditors;
            this.cacheStore = cacheStore;
            this.logger = logger;
        }

        public abstract string AccountingPrincipal { get; }

        public async Task MakeAccountingAsync()
        {
            //REMARK (5he11) 取以当前时间为基础的昨天的最后一刻并转化为UTC时间，规范后的值如：2018-08-08 23:59:59.999 +00:00
            var targetTime = new DateTimeOffset(DateTimeOffset.UtcNow.UtcDateTime.Date.ToUniversalTime()).AddMilliseconds(-1);
            var latest = await this.GetLatestAccountingAsync(targetTime);
            if (latest.ETag == null)
            {
                //REMARK (5he11) ETag为空指示系统中未找到任何数据，即新账号，仅生成昨天的记录即可
                await this.MakeAccountingAsync(targetTime, 0);
            }
            else
            {
                var pos = latest.GetEnd();
                var previousBalance = latest.Balance;
                while (pos < targetTime)
                {
                    //REMARK (5he11) 数据层存储的都是记账日当天最后一刻的时间，所以加1毫秒就是新的要创建账务的日期
                    var buildTarget = pos.AddMilliseconds(1);
                    var accounting = await this.MakeAccountingAsync(buildTarget, previousBalance);
                    if (accounting == null)
                    {
                        break;
                    }
                    previousBalance = accounting.Balance;
                    pos = pos.AddDays(1);
                }
            }
        }

        public IAsyncEnumerable<Transaction> GetTransactionsAsync(DateTimeOffset fromInclusive, DateTimeOffset toInclusive)
        {
            var principal = this.AccountingPrincipal;
            return transactionRepo.Value.QueryAsync(
                      Transaction.BuildPartitionKey(principal),
                      Transaction.BuildRowKey(toInclusive),
                      Transaction.BuildRowKey(fromInclusive));
        }

        public async Task<double> GetFrozenAsync()
        {
            var pk = FrozenKey;
            var rk = this.AccountingPrincipal;
            var result = await this.cacheStore.Value.GetAsync<double>(pk, rk);
            return result;
        }

        public async Task<double> FreezeAsync(double amount)
        {
            amount = amount.ChineseRound();
            if (amount < 0)
            {
                throw new ArgumentException("金额不应为负值", nameof(amount));
            }
            var pk = FrozenKey;
            var rk = this.AccountingPrincipal;
            var currentValue = await this.cacheStore.Value.GetAsync<double>(pk, rk);
            var result = currentValue + amount;
            await this.cacheStore.Value.SetAsync(pk, rk, result, false);
            return result;
        }

        public async Task<double> UnfreezeAsync()
        {
            var pk = FrozenKey;
            var rk = this.AccountingPrincipal;
            await this.cacheStore.Value.DeleteAsync(pk, rk);
            return 0;
        }

        public async Task<double> UnfreezeAsync(double amount)
        {
            amount = amount.ChineseRound();
            if (amount < 0)
            {
                throw new ArgumentException("金额不应为负值", nameof(amount));
            }
            var pk = FrozenKey;
            var rk = this.AccountingPrincipal;
            var currentValue = await this.cacheStore.Value.GetAsync<double>(pk, rk);
            var result = currentValue - amount;
            await this.cacheStore.Value.SetAsync(pk, rk, result, false);
            return result;
        }

        public Task<TransactionRequest> BuildTransactionAsync(
            double delta, int reason, string remark, string reference, string id = null, string corelation = null)
            => Task.FromResult(new TransactionRequest(this.AccountingPrincipal, delta)
            {
                Reason = reason,
                ID = id,
                Reference = reference,
                Remark = remark,
                Corelation = corelation,
            });

        public async Task<IEnumerable<Transaction>> MakeTransactionAsync(
            double delta, int reason, string remark, string reference, string id = null, string corelation = null)
            => await MakeTransactionAsync(new[] { await this.BuildTransactionAsync(delta, reason, remark, reference, id, corelation) });

        public async Task<IEnumerable<Transaction>> MakeTransactionAsync(TransactionRequest request)
          => await MakeTransactionAsync(new[] { request });

        //TODO (5he11) 此方法要加锁并且实现事务
        public async Task<IEnumerable<Transaction>> MakeTransactionAsync(IEnumerable<TransactionRequest> requests)
        {
            var transactions = new List<Transaction>();
            var count = 0;
            foreach (var request in requests)
            {
                if (String.IsNullOrWhiteSpace(request.Target))
                {
                    throw new ArgumentNullException(nameof(requests));
                }

                request.ID ??= Transaction.BuildRowKey(DateTimeOffset.UtcNow);
                request.Delta = request.Delta.ChineseRound();
                request.Target = request.Target.Trim();

                var transaction = new Transaction
                {
                    Delta = request.Delta,
                    Remark = request.Remark,
                    Reason = request.Reason,
                    Reference = request.Reference,
                    Corelation = request.Corelation,
                };
                transaction.SetOwner(request.Target);
                transaction.RowKey = request.ID;
                transactions.Add(transaction);
                count++;
            }

            await transactionRepo.Value.CreateAsync(transactions);
            var now = DateTimeOffset.UtcNow.ToSixDigitsDate();
            foreach (var transaction in transactions)
            {
                var pk = $"{DeltaKey}-{now}";
                var rk = transaction.GetOwner();
                var currentValue = await cacheStore.Value.GetAsync<double>(pk, rk);
                var result = (currentValue + transaction.Delta).ChineseRound();
                await cacheStore.Value.SetAsync(pk, rk, result, false);
            }
            return transactions;
        }

        public async Task<Transaction> GetTransactionAsync(DateTimeOffset id)
        {
            var target = this.AccountingPrincipal;
            var transaction = await transactionRepo.Value.RetrieveAsync(target, Transaction.BuildRowKey(id));
            return transaction;
        }

        public async Task<AccountBalance> GetBalanceAsync(DateTimeOffset input)
        {
            //REMARK (5he11) 将输入限制为仅取其日期的当日的最后一刻并转化为UTC时间，规范后的值如：2018-08-08 23:59:59.999 +00:00
            input = new DateTimeOffset(input.UtcDateTime.Date.ToUniversalTime()).AddDays(1).AddMilliseconds(-1);
            var frozen = await this.GetFrozenAsync();
            frozen = frozen.ChineseRound();
            bool queryCache;
            var lastAccountDate = input;
            if (input.UtcDateTime.Date.ToUniversalTime() != DateTimeOffset.UtcNow.UtcDateTime.Date.ToUniversalTime())
            {
                //REMARK (5he11) 提高执行效率，不查询当天的值可以尝试是否可以直接命中
                queryCache = false;
            }
            else
            {
                lastAccountDate = lastAccountDate.AddDays(-1);
                //REMARK (5he11) 提高执行效率，查询当天的值时，首先尝试直接命中前一天晚上临结束时的准确账目
                queryCache = true;
            }

            double balance;
            var principal = this.AccountingPrincipal;
            var accounting = await accountingRepo.Value.RetrieveAsync(Accounting.BuildPartitionKey(principal), Accounting.BuildRowKey(lastAccountDate));
            if (accounting == null)
            {
                //REMARK (5he11) 若无法高效命中则使用慢速范围查询
                accounting = await this.GetLatestAccountingAsync(lastAccountDate);
                queryCache = true;
            }
            balance = accounting.Balance;

            if (queryCache)
            {
                //REMARK (5he11) 增加1毫秒使起始值规范为第2天的0点，以此避免将以日期命名的缓存多加一遍
                //REMARK (5he11) 但如果结果的 ETag 为空则表示是新用户，需累加其所有缓存的值即可，这里的做法是累加3天，而且输入时间减去1毫秒让循环至少执行一次
                var pos = accounting.ETag == null ? input.AddMilliseconds(-1).AddDays(-3) : accounting.GetEnd().AddMilliseconds(1);
                while (pos < input)
                {
                    var delta = await this.GetDeltaAsync(pos);
                    balance += delta;
                    pos = pos.AddDays(1);
                }
            }

            balance = balance.ChineseRound();

            return new AccountBalance
            {
                Total = balance,
                Frozen = frozen,
                Available = balance - frozen
            };
        }

        private async Task<double> GetDeltaAsync(DateTimeOffset input)
        {
            var pk = $"{DeltaKey}-{input.ToSixDigitsDate()}";
            var rk = this.AccountingPrincipal;
            var result = await this.cacheStore.Value.GetAsync<double>(pk, rk);
            return result.ChineseRound();
        }

        private async Task ClearDeltaAsync(DateTimeOffset input)
        {
            var pk = $"{DeltaKey}-{input.ToSixDigitsDate()}";
            var rk = this.AccountingPrincipal;
            await this.cacheStore.Value.DeleteAsync(pk, rk);
        }

        private async Task<Accounting> MakeAccountingAsync(DateTimeOffset input, double previousBalance)
        {
            //REMARK (5he11) 将输入限制为仅取其日期的当日的最后一刻并转化为UTC时间，规范后的值如：2018-08-08 23:59:59.999 +00:00
            input = new DateTimeOffset(input.UtcDateTime.Date.ToUniversalTime()).AddDays(1).AddMilliseconds(-1);
            if (input > DateTimeOffset.UtcNow)
            {
                //REMARK (5he11) 生成账务的时间不应该比当前时间靠后，否则会造成账务错误
                return null;
            }

            //REMARK (5he11) 取其日期的当日的第一刻并转化为UTC时间，规范后的值如：2018-08-08 00:00:00.000 +00:00
            var transactionSearchFrom = new DateTimeOffset(input.UtcDateTime.Date.ToUniversalTime());
            var transactions = await this.GetTransactionsAsync(transactionSearchFrom, input).ToListAsync();

            var credits = transactions.Where(t => t.Delta > 0).Select(t => t.Delta).DefaultIfEmpty().Sum();
            var debits = transactions.Where(t => t.Delta < 0).Select(t => t.Delta).DefaultIfEmpty().Sum();
            var delta = await this.GetDeltaAsync(input);
            var diff = credits + debits - delta;
            var b = (previousBalance + credits + debits).ChineseRound();
            var principal = this.AccountingPrincipal;
            this.logger.LogInformation($"账务主体 {principal} 从 {transactionSearchFrom} 开始，截止到 {input} 为止的账务变化为 {credits}/{debits} 与该日缓存相差 {diff} 截止此时余额为 {b}");

            //if (Math.Abs(diff) > 1)
            //{
            //    //TODO (5he11) 发邮件报警
            //    return null;
            //}

            var accounting = new Accounting
            {
                Balance = (previousBalance + credits + debits).ChineseRound(),
                Credits = credits.ChineseRound(),
                Debits = debits.ChineseRound(),
                Created = DateTimeOffset.UtcNow,
            };
            accounting.SetPrincipal(principal);
            accounting.SetEnd(input);

            foreach (var auditor in auditors.Value)
            {
                await auditor.AuditAsync(accounting, transactions);
            }

            await accountingRepo.Value.CreateAsync(accounting);
            await this.ClearDeltaAsync(input);
            return accounting;
        }

        private async Task<Accounting> GetLatestAccountingAsync(DateTimeOffset input)
        {
            //REMARK (5he11) 将输入限制为仅取其日期的当日的最后一刻并转化为UTC时间，规范后的值如：2018-08-08 23:59:59.999 +00:00
            input = new DateTimeOffset(input.UtcDateTime.Date.ToUniversalTime()).AddDays(1).AddMilliseconds(-1);
            var principal = this.AccountingPrincipal;
            var searchFrom = input.AddDays(-30);
            var accountings = await accountingRepo.Value.QueryAsync(
                                    Accounting.BuildPartitionKey(principal),
                                    Accounting.BuildRowKey(input),
                                    Accounting.BuildRowKey(searchFrom))
                                .ToListAsync();
            var latest = accountings.OrderByDescending(a => a.GetEnd()).FirstOrDefault();
            if (latest != null)
            {
                return latest;
            }
            var empty = new Accounting
            {
                Balance = 0,
                Credits = 0,
                Debits = 0
            };
            empty.SetEnd(input);
            empty.SetPrincipal(principal);
            return empty;
        }
    }
}